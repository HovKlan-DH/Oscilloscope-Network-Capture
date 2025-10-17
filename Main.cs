using Ookii.Dialogs.WinForms;
using Oscilloscope_Network_Capture.Core.Configuration;
using Oscilloscope_Network_Capture.Core.Logging;
using Oscilloscope_Network_Capture.Core.Online;
using Oscilloscope_Network_Capture.Core.Scopes;
using Oscilloscope_Network_Capture.Core.Scopes.Implementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Oscilloscope_Network_Capture
{
    public partial class Main : Form
    {
        // Overlay shown on picScreen after delete
        private Label _deleteOverlayLabel;
        private System.Windows.Forms.Timer _deleteOverlayTimer;
        
        private enum KeyActionType { None = 0, AdjustTrigger = 1, AdjustTimeDiv = 2, Other = 3 }
        private KeyActionType _activeKeyAction = KeyActionType.None;
        private int _keyActionBusy; // 0 = idle, 1 = a capture-mode key action is running

        public static string versionThis = "";
        private string versionOnline = "";

        private int _timeDivAdjustBusy;
        private Panel _enterFlashOverlay;
        private System.Windows.Forms.Timer _enterFlashTimer;
        private System.Windows.Forms.Timer _connectionMonitorTimer;
        private int _connectionCheckBusy; // 0 = idle, 1 = monitor tick in-flight
        private bool _isConnectable = false; // true only after a successful connection; false on failure/loss
        private int _connFailStreak;                         // consecutive heartbeat failures
        private const int ConnectionFailThreshold = 3;       // require N failures before disabling
        private DateTime _lastHeartbeatUtc;                  // last successful heartbeat timestamp

        // Trigger-level adjust pump (coalesces rapid keypresses)
        private int _triggerAdjustPending;                 // net steps (+/-)
        private Task _triggerAdjustWorker;                 // current pump task
        private readonly SemaphoreSlim _triggerAdjustGate = new SemaphoreSlim(1, 1);
        private const int TriggerAdjustDebounceMs = 70;    // collect rapid key presses
        private const int TriggerAdjustMinIntervalMs = 80; // guard between instrument writes

        private ErrorProvider _emailErrorProvider;
        private AppConfiguration _config;
        private IScope _scope;
        private CancellationTokenSource _cts;
        private bool _hadConfigOnStartup;
        private System.Windows.Forms.Timer _emailSaveDebounceTimer;
        private bool _suppressLayoutSave = true; // prevent saving during initial layout restore
        private System.Windows.Forms.Timer _filenameSaveDebounceTimer; // debounce filename format saves

        private readonly Queue<LogEventArgs> _logQueue = new Queue<LogEventArgs>();
        private readonly object _logSync = new object();
        private bool _logDrainPending;

        private const string VarLabelPrefix = "varLbl";
        private const string VarTextPrefix = "varTxt";
        private const string VarNameEditorPrefix = "varName";
        private const string VarNameLabelPrefix = "varNameLbl"; // labels for Misc tab variable name editors

        // History of saved filenames (session-only)
        private readonly List<string> _savedFileHistory = new List<string>();

        // Capture mode state and UI refs
        private bool _captureMode = false;
        private Button _btnCaptureStart;
        private TextBox _tbCaptureFolder;
        private TextBox _tbFilenameFormat;

        public Main()
        {
            InitializeComponent();
            Logger.Instance.MessageLogged += Logger_MessageLogged;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Make sure the initial indicators reflect default state immediately
            UpdateCaptureModeIndicators();
        }

        private void Logger_MessageLogged(object sender, LogEventArgs e)
        {
            if (IsDisposed) return;
            lock (_logSync)
            {
                _logQueue.Enqueue(e);
                if (_logDrainPending) return;
                _logDrainPending = true;
            }
            try
            {
                BeginInvoke(new Action(DrainLogQueue));
            }
            catch
            {
                // Form might be disposing; drop logs
                lock (_logSync) { _logQueue.Clear(); _logDrainPending = false; }
            }
        }

        private void DrainLogQueue()
        {
            while (true)
            {
                LogEventArgs next = null;
                lock (_logSync)
                {
                    if (_logQueue.Count > 0)
                    {
                        next = _logQueue.Dequeue();
                    }
                    else
                    {
                        _logDrainPending = false;
                        break;
                    }
                }
                try { AppendLog(next); } catch { }
            }
        }

        private static string InnermostMessage(Exception ex)
        {
            if (ex == null) return string.Empty;
            while (ex.InnerException != null) ex = ex.InnerException;
            return ex.Message ?? ex.ToString();
        }

        private void AppendLog(LogEventArgs e)
        {
            if (rtbLog.IsDisposed) return;
            // Show exactly the same line as written to logfile
            string line = e.ToString();

            // Mask serial in IDN responses for Debug tab if enabled
            if (_config != null && _config.MaskSerial)
                line = MaskSerialInIdnDebugLine(line);

            rtbLog.SelectionColor = GetLogColor(e.Level);
            rtbLog.AppendText(line + Environment.NewLine);
            rtbLog.SelectionColor = rtbLog.ForeColor; // restore default
            rtbLog.ScrollToCaret();
        }

        private static Color GetLogColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return Color.DarkSeaGreen;   // requested
                case LogLevel.Info:  return Color.LightGreen;     // requested
                case LogLevel.Error: return Color.LightPink;      // requested
                default:             return Color.Gainsboro;
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            GetAssemblyVersion();
            CheckForNewVersionAsync();

            // Ensure initial indicator state (inactive shown by default)
            UpdateCaptureModeIndicators();

            // Capture whether a config file existed at startup (before any saves)
            _hadConfigOnStartup = ConfigurationService.Exists;

            _config = ConfigurationService.Load();
            Logger.Instance.MinimumLevel = (LogLevel)_config.LogLevel;

            // Restore window layout as early as possible, but suppress saving during this phase
            RestoreWindowLayout();

            // Read designer default for DelayMs so we don't ever persist -1 on first save
            var numDelayAtLoad = this.Controls.Find("numericUpDownDelayMs", true).FirstOrDefault() as NumericUpDown
                                  ?? this.Controls.Find("numericUpDown2", true).FirstOrDefault() as NumericUpDown;
            int designerDelayMs = numDelayAtLoad != null ? (int)numDelayAtLoad.Value : 0;

            // If first launch, initialize default variables and DelayMs from designer
            if (!_hadConfigOnStartup)
            {
                _config.VariableCount = 2;
                //_config.VariableNames = new List<string> { "COMPONENT", "REGION" };
                _config.VariableNames = new List<string> { "COMPONENT" };
                //_config.VariableValues = new List<string> { "U1", "PAL" };
                _config.VariableValues = new List<string> { "U1" };
                _config.DelayMs = designerDelayMs; // never persist -1
                // Default capture folder to executable directory
                _config.CaptureFolder = Application.StartupPath;
                _config.EnableDelete = false; // or true, your choice
                ConfigurationService.Save(_config);
            }

            // Default to Debug during development (do not save here)
            Logger.Instance.MinimumLevel = LogLevel.Debug;
            _config.LogLevel = (int)LogLevel.Debug;

            // Restore last tab
            if (_config.LastTabIndex >= 0 && _config.LastTabIndex < tabMain.TabPages.Count)
                tabMain.SelectedIndex = _config.LastTabIndex;

            // Set console-like colors for the log view
            rtbLog.BackColor = Color.Black;
            rtbLog.ForeColor = Color.Gainsboro;

            // Load IP/Port defaults
            txtIp.Text = string.IsNullOrWhiteSpace(_config.ScopeIp) ? "192.168.0.100" : _config.ScopeIp;
            if (_config.ScopePort <= 0) _config.ScopePort = 5025;
            numPort.Value = _config.ScopePort;

            // Persist when user changes connection settings
            txtIp.Leave += (s, a) => SaveConnectionSettings();
            numPort.ValueChanged += (s, a) => SaveConnectionSettings();

            // Save tab selection changes
            tabMain.SelectedIndexChanged += (s, a) => { _config.LastTabIndex = tabMain.SelectedIndex; ConfigurationService.Save(_config); };

            // Populate vendors/models
            var descriptors = ScopeFactory.GetAvailableScopes().ToList();
            var vendors = descriptors.Select(d => d.Vendor).Distinct().OrderBy(v => v).ToList();
            cboVendor.Items.AddRange(vendors.Cast<object>().ToArray());
            if (!string.IsNullOrEmpty(_config.Vendor) && vendors.Contains(_config.Vendor))
                cboVendor.SelectedItem = _config.Vendor;
            else if (vendors.Count > 0)
                cboVendor.SelectedIndex = 0;

            cboVendor.SelectedIndexChanged += (s, a) =>
            {
                _config.Vendor = cboVendor.SelectedItem as string ?? string.Empty;
                ApplyVendorDefaultPort();          // optional: change default port per vendor
                UpdateModelsForVendor();           // rebuild model list for this vendor
                PopulateCommandTextboxes();        // load default SCPI for selected vendor/model
                ApplyScpiOverridesForCurrentProfile(); // then apply user overrides (if any)
                UpdateScpiHeaderLabel();
                PopulateTimeDivTextbox();
                ConfigurationService.Save(_config);
            };

            cboModel.SelectedIndexChanged += (s, a) =>
            {
                var modelDisplay = cboModel.SelectedItem as string ?? string.Empty;

                // Persist exact series name
                _config.Model = modelDisplay;

                PopulateCommandTextboxes();
                ApplyScpiOverridesForCurrentProfile();
                UpdateScpiHeaderLabel();
                PopulateTimeDivTextbox();

                Oscilloscope_Network_Capture.Core.Configuration.ConfigurationService.Save(_config);
            };

            // Then do the initial populate:
            UpdateModelsForVendor();
            PopulateCommandTextboxes();
            ApplyScpiOverridesForCurrentProfile();
            UpdateScpiHeaderLabel();
            PopulateTimeDivTextbox();

            // Wire once
            WireTimeDivTextbox();

            // Prepare error provider for email validation
            _emailErrorProvider = new ErrorProvider();
            _emailErrorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            // Restore saved email if control exists
            if (this.Controls.Find("textBoxEmail", true).FirstOrDefault() is TextBox tbEmail)
            {
                tbEmail.Text = _config.Email ?? string.Empty;
                tbEmail.Leave += (s, a) => { _config.Email = tbEmail.Text?.Trim(); ConfigurationService.Save(_config); };

                // Debounced save on text change
                _emailSaveDebounceTimer = new System.Windows.Forms.Timer { Interval = 600 };
                _emailSaveDebounceTimer.Tick += (s, a) =>
                {
                    _emailSaveDebounceTimer.Stop();
                    _config.Email = tbEmail.Text?.Trim();
                    ConfigurationService.Save(_config);
                };
                tbEmail.TextChanged += (s, a) =>
                {
                    _emailSaveDebounceTimer.Stop();
                    _emailSaveDebounceTimer.Start();
                };

                InitializeTriggerSetButtonState();
            }

            if (numericUpDown1 != null)
            {
                // Keep designer default; just update preview when changed
                numericUpDown1.ValueChanged += (s, a) =>
                {
                    UpdateActionRichTextForNextFilename();
                };
            }

            // Filename format textbox: load from config, default to designer text on first run; debounce saves
            _tbFilenameFormat = this.Controls.Find("textBoxFilenameFormat", true).FirstOrDefault() as TextBox
                        ?? this.Controls.Find("textBox1", true).FirstOrDefault() as TextBox;
            if (_tbFilenameFormat != null)
            {
                if (string.IsNullOrWhiteSpace(_config.FilenameFormat))
                {
                    // keep whatever designer put as default on first run
                    _config.FilenameFormat = _tbFilenameFormat.Text ?? string.Empty;
                    ConfigurationService.Save(_config);
                }
                else
                {
                    _tbFilenameFormat.Text = _config.FilenameFormat ?? string.Empty;
                }

                _filenameSaveDebounceTimer = new System.Windows.Forms.Timer { Interval = 400 };
                _filenameSaveDebounceTimer.Tick += (s, a) =>
                {
                    _filenameSaveDebounceTimer.Stop();
                    _config.FilenameFormat = _tbFilenameFormat.Text ?? string.Empty;
                    ConfigurationService.Save(_config);
                };
                _tbFilenameFormat.TextChanged += (s, a) =>
                {
                    _filenameSaveDebounceTimer.Stop();
                    _filenameSaveDebounceTimer.Start();
                    UpdateActionRichTextForNextFilename();
                };
                _tbFilenameFormat.Leave += (s, a) =>
                {
                    _filenameSaveDebounceTimer.Stop();
                    _config.FilenameFormat = _tbFilenameFormat.Text ?? string.Empty;
                    ConfigurationService.Save(_config);
                    UpdateActionRichTextForNextFilename();
                };
            }

            // Variables (Misc tab and Measurements tab)
            if (numericUpDownVariables != null)
            {
                numericUpDownVariables.Minimum = 0;
                numericUpDownVariables.Maximum = 5;
                numericUpDownVariables.ValueChanged += (s, a) =>
                {
                    _config.VariableCount = (int)numericUpDownVariables.Value;
                    EnsureVariableNamesListSize();
                    ConfigurationService.Save(_config);
                    BuildVariableNameEditors();
                    UpdateVariablesUI(_config.VariableCount);
                    UpdateActionRichTextForNextFilename();
                };
                numericUpDownVariables.Value = Math.Max(0, Math.Min(5, _config.VariableCount));
            }
            EnsureVariableNamesListSize();
            BuildVariableNameEditors();
            UpdateVariablesUI(_config.VariableCount);

            // Load and wire Misc tab options
            var chkBeep = this.Controls.Find("checkBoxEnableBeep", true).FirstOrDefault() as CheckBox
                           ?? this.Controls.Find("checkBox1", true).FirstOrDefault() as CheckBox;
            var chkForceAcq = this.Controls.Find("checkBoxForceAcquisition", true).FirstOrDefault() as CheckBox
                               ?? this.Controls.Find("checkBox2", true).FirstOrDefault() as CheckBox;
            var chkForceClear = this.Controls.Find("checkBoxForceClear", true).FirstOrDefault() as CheckBox
                                 ?? this.Controls.Find("checkBox3", true).FirstOrDefault() as CheckBox;
            var chkDoNotClearWhenStop = this.Controls.Find("checkBoxDoNotClearWhenStop", true).FirstOrDefault() as CheckBox;
            var numDelay = this.Controls.Find("numericUpDownDelayMs", true).FirstOrDefault() as NumericUpDown
                           ?? this.Controls.Find("numericUpDown2", true).FirstOrDefault() as NumericUpDown;
            var chkDelDU = this.Controls.Find("checkBoxDeleteDoubleUnderscore", true).FirstOrDefault() as CheckBox;
            var chkTrim = this.Controls.Find("checkBoxTrimUnderscore", true).FirstOrDefault() as CheckBox;

            // Newly added: Enable Delete option
            var chkEnableDelete = this.Controls.Find("checkBoxEnableDelete", true).FirstOrDefault() as CheckBox;
            if (chkEnableDelete != null)
            {
                chkEnableDelete.Checked = _config.EnableDelete;
                chkEnableDelete.CheckedChanged += (s, a) =>
                {
                    _config.EnableDelete = chkEnableDelete.Checked;
                    ConfigurationService.Save(_config);
                };
            }

            // Newly added: Mask Serial option
            checkBoxMaskSerial.Checked = _config.MaskSerial;
            checkBoxMaskSerial.CheckedChanged += (s, a) =>
            {
                _config.MaskSerial = checkBoxMaskSerial.Checked;
                ConfigurationService.Save(_config);
            };

            if (chkBeep != null)
            {
                chkBeep.Checked = _config.EnableBeep;
                chkBeep.CheckedChanged += (s, a) => { _config.EnableBeep = chkBeep.Checked; ConfigurationService.Save(_config); };
            }
            if (chkForceAcq != null)
            {
                chkForceAcq.Checked = _config.ForceAcquisition;
                chkForceAcq.CheckedChanged += (s, a) => { _config.ForceAcquisition = chkForceAcq.Checked; ConfigurationService.Save(_config); };
            }
            if (chkForceClear != null)
            {
                chkForceClear.Checked = _config.ForceClear;

                // Sync on load
                if (numDelay != null) numDelay.Enabled = chkForceClear.Checked;
                if (chkDoNotClearWhenStop != null) chkDoNotClearWhenStop.Enabled = chkForceClear.Checked;
                chkForceClear.CheckedChanged += (s, a) =>
                {
                    _config.ForceClear = chkForceClear.Checked;
                    ConfigurationService.Save(_config);

                    // Toggle both controls when ForceClear changes
                    if (numDelay != null) numDelay.Enabled = chkForceClear.Checked;
                    if (chkDoNotClearWhenStop != null) chkDoNotClearWhenStop.Enabled = chkForceClear.Checked;
                };
            }
            if (chkDelDU != null)
            {
                chkDelDU.Checked = _config.DeleteDoubleUnderscore;
                chkDelDU.CheckedChanged += (s, a) => { _config.DeleteDoubleUnderscore = chkDelDU.Checked; ConfigurationService.Save(_config); UpdateActionRichTextForNextFilename(); };
            }
            if (chkTrim != null)
            {
                chkTrim.Checked = _config.TrimUnderscore;
                chkTrim.CheckedChanged += (s, a) => { _config.TrimUnderscore = chkTrim.Checked; ConfigurationService.Save(_config); UpdateActionRichTextForNextFilename(); };
            }
            if (numDelay != null)
            {
                // Only override UI value if we have a stored one; otherwise respect designer default
                if (_config.DelayMs >= 0)
                {
                    numDelay.Value = Math.Min(numDelay.Maximum, Math.Max(numDelay.Minimum, _config.DelayMs));
                }
                numDelay.ValueChanged += (s, a) =>
                {
                    var newVal = (int)numDelay.Value;
                    if (newVal < 0) newVal = 0; // never save -1 or negatives
                    _config.DelayMs = newVal;
                    ConfigurationService.Save(_config);
                };
            }

            // Capture folder UI
            _tbCaptureFolder = this.Controls.Find("textBoxCaptureOutputFolder", true).FirstOrDefault() as TextBox
                               ?? this.Controls.Find("textBox2", true).FirstOrDefault() as TextBox;
            if (_tbCaptureFolder != null)
            {
                if (!string.IsNullOrWhiteSpace(_config.CaptureFolder))
                    _tbCaptureFolder.Text = _config.CaptureFolder;
                else if (!_hadConfigOnStartup)
                    _tbCaptureFolder.Text = _config.CaptureFolder; // set to startup path from first run block

                _tbCaptureFolder.ReadOnly = true; // prevent manual edits; enforce picker
                _tbCaptureFolder.Cursor = Cursors.Hand;
                _tbCaptureFolder.Click += (s, a) => SelectCaptureFolder(_tbCaptureFolder);
            }

            // Wire capture start button (prefer new name, fallback to legacy button1)
            buttonCaptureStart.Click -= async (s, a) => await StartCaptureModeAsync();
            buttonCaptureStart.Click += async (s, a) =>
            {
                await RunConnectivityCheckAsync(_btnCaptureStart);
            };

            // Persist DoNotClearWhenStop state
            if (chkDoNotClearWhenStop != null)
            {
                chkDoNotClearWhenStop.Checked = _config.DoNotClearWhenStop;
                chkDoNotClearWhenStop.CheckedChanged += (s, a) =>
                {
                    _config.DoNotClearWhenStop = chkDoNotClearWhenStop.Checked;
                    ConfigurationService.Save(_config);
                };
            }

            // Enable layout persistence after initial show
            _suppressLayoutSave = false;

            if (tabSettings != null)
            {
                tabSettings.AutoScroll = true;
            }

            // Ensure all version badges are placed top-right within their tab
            ApplyTopRightBadges();

            // Initialize action text with current next filename
            UpdateActionRichTextForNextFilename();

            // Persist/restore Adjust-to-grid combobox
            WireAdjustToGridComboPersistence();

            PopulateHelpTabRtf();
            PopulateAboutTabRtf();

            // Make hyperlinks clickable in both Help and About
            WireRichTextBoxHyperlinks("richTextBoxHelp", "richTextBoxAbout");

            if (_hadConfigOnStartup)
            {
                BeginInvoke(new Action(async () =>
                {
                    try { await ConnectAndRefreshAsync(); } catch { }
                    await AutoStartCaptureModeIfConnectedAsync();
                }));
            }
        }

        private void buttonOpenFolder_Click(object sender, EventArgs e)
        {
            OpenCaptureFolder();
            removeFocus();
        }

        private Task ExecuteCaptureKeyAsync(Func<Task> action)
        {
            if (action == null) return Task.CompletedTask;

            // Reject if another key action is in-flight
            if (Interlocked.CompareExchange(ref _keyActionBusy, 1, 0) != 0)
            {
                Logger.Instance.Debug("Keyboard command ignored, as another operation is in progress.");
                return Task.CompletedTask;
            }

            return Run();

            async Task Run()
            {
                try
                {
                    await action().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Key action failed: " + InnermostMessage(ex));
                }
                finally
                {
                    Interlocked.Exchange(ref _keyActionBusy, 0);
                }
            }
        }

        // Add an overload that allows "speedy access" coalescing for same action type
        private Task ExecuteCaptureKeyAsync(Func<Task> action, KeyActionType type, Action coalesceIfBusy)
        {
            if (action == null) return Task.CompletedTask;

            // If something is running:
            if (Interlocked.CompareExchange(ref _keyActionBusy, 1, 0) != 0)
            {
                // If it's the same kind of action, coalesce (e.g., add another +/- step) and return fast
                if (_activeKeyAction == type && coalesceIfBusy != null)
                {
                    coalesceIfBusy();
                    return Task.CompletedTask;
                }

                Logger.Instance.Debug("Keyboard command ignored, as another operation is in progress.");
                return Task.CompletedTask;
            }

            _activeKeyAction = type;
            return Run();

            async Task Run()
            {
                try
                {
                    await action().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Key action failed: " + InnermostMessage(ex));
                }
                finally
                {
                    _activeKeyAction = KeyActionType.None;
                    Interlocked.Exchange(ref _keyActionBusy, 0);
                }
            }
        }

        private void OpenCaptureFolder()
        {
            try
            {
                var path = _config.CaptureFolder;
                if (string.IsNullOrWhiteSpace(path)) path = Application.StartupPath;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open folder: " + InnermostMessage(ex), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectCaptureFolder(TextBox target)
        {
            try
            {
                using (var dlg = new VistaFolderBrowserDialog())
                {
                    dlg.Description = "Select capture folder";
                    dlg.UseDescriptionForTitle = true;
                    if (!string.IsNullOrWhiteSpace(_config.CaptureFolder) && Directory.Exists(_config.CaptureFolder))
                        dlg.SelectedPath = _config.CaptureFolder;

                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        var path = dlg.SelectedPath?.Trim();
                        if (!string.IsNullOrEmpty(path))
                        {
                            target.Text = path;
                            _config.CaptureFolder = path;
                            ConfigurationService.Save(_config);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to select folder: " + InnermostMessage(ex), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveConnectionSettings()
        {
            if (_config == null) return;
            _config.ScopeIp = txtIp.Text?.Trim();
            _config.ScopePort = (int)numPort.Value;
            ConfigurationService.Save(_config);
        }

        private string ComposeResource()
        {
            var ip = (txtIp.Text ?? string.Empty).Trim();
            var port = (int)numPort.Value;
            return string.Format("{0}:{1}", ip, port);
        }

        // Add this helper near the other private helpers (below ComposeResource / ToModelPattern region)
        private void SetConnectivityStatus(string text, Color color)
        {
            try
            {
                if (lblStatus != null)
                {
                    lblStatus.Text = text ?? string.Empty;
                    lblStatus.ForeColor = color;
                }

                var alt = this.Controls.Find("label15", true).FirstOrDefault() as Label;
                if (alt != null)
                {
                    alt.Text = text ?? string.Empty;
                    alt.ForeColor = color;
                }
            }
            catch { /* best-effort */ }
        }

        // Add near SetConnectivityStatus
        private void SetConnectButtonsEnabled(bool enabled)
        {
            try
            {
                if (btnConnect != null) btnConnect.Enabled = enabled;

                // Ensure we have a handle to buttonCaptureStart
                if (_btnCaptureStart == null)
                {
                    _btnCaptureStart = this.Controls.Find("buttonCaptureStart", true).FirstOrDefault() as Button
                                       ?? this.Controls.Find("button1", true).FirstOrDefault() as Button;
                }
                if (_btnCaptureStart != null) _btnCaptureStart.Enabled = enabled;
            }
            catch { }
        }

        // Shared connectivity flow for any button that should behave like "Connect"
        private async Task RunConnectivityCheckAsync(Button selfButton)
        {
            try
            {
                SetConnectButtonsEnabled(false);
                Cursor = Cursors.WaitCursor;

                await WithOtherTestButtonsDisabledAsync(selfButton, async () =>
                {
                    await ConnectAndRefreshAsync();
                });

                await AutoStartCaptureModeIfConnectedAsync();
            }
            finally
            {
                SetConnectButtonsEnabled(true);
                Cursor = Cursors.Default;
                if (selfButton != null && selfButton.CanFocus) selfButton.Focus();
            }
        }

        private static string ToDisplayModel(string modelPattern) => modelPattern == "*" ? "Generic" : modelPattern;
        private static string ToModelPattern(string displayModel) => string.Equals(displayModel, "Generic", StringComparison.OrdinalIgnoreCase) ? "*" : displayModel;

        /*
        private ScpiProfileOverride GetOrCreateCurrentProfileOverride()
        {
            var vendor = cboVendor.SelectedItem as string ?? string.Empty;
            var modelDisplay = cboModel.SelectedItem as string ?? string.Empty;
            var model = ToModelPattern(modelDisplay);
            var prof = _config.ScpiProfiles.FirstOrDefault(p => string.Equals(p.Vendor, vendor, StringComparison.OrdinalIgnoreCase) && string.Equals(p.Model, model, StringComparison.OrdinalIgnoreCase));
            if (prof == null)
            {
                prof = new ScpiProfileOverride { Vendor = vendor, Model = model };
                _config.ScpiProfiles.Add(prof);
            }
            return prof;
        }
        */

        private ScpiProfileOverride FindCurrentProfileOverride()
        {
            if (_config == null) return null;
            var vendor = cboVendor.SelectedItem as string ?? string.Empty;
            var modelDisplay = cboModel.SelectedItem as string ?? string.Empty;
            var model = ToModelPattern(modelDisplay);
            return _config.ScpiProfiles.FirstOrDefault(p => string.Equals(p.Vendor, vendor, StringComparison.OrdinalIgnoreCase)
                                                         && string.Equals(p.Model, model, StringComparison.OrdinalIgnoreCase));
        }

        private string ResolveUiScpiFor(ScopeCommand cmd)
        {
            switch (cmd)
            {
                case ScopeCommand.Identify: return (txtCmdIdentify?.Text ?? string.Empty).Trim();
                case ScopeCommand.DrainSystemErrorQueue: return (txtCmdSysErr?.Text ?? string.Empty).Trim();
                case ScopeCommand.OperationComplete: return (txtCmdOpc?.Text ?? string.Empty).Trim();
                case ScopeCommand.ClearStatistics: return (txtCmdClearStats?.Text ?? string.Empty).Trim();
                case ScopeCommand.QueryActiveTrigger: return (txtCmdActiveTrig?.Text ?? string.Empty).Trim();
                case ScopeCommand.Stop: return (txtCmdStop?.Text ?? string.Empty).Trim();
                case ScopeCommand.Single: return (txtCmdSingle?.Text ?? string.Empty).Trim();
                case ScopeCommand.Run: return (txtCmdRun?.Text ?? string.Empty).Trim();
                case ScopeCommand.QueryTriggerMode: return (txtCmdTrigMode?.Text ?? string.Empty).Trim();
                case ScopeCommand.QueryTriggerLevel: return (txtCmdTrigLevelQ?.Text ?? string.Empty).Trim();
                case ScopeCommand.SetTriggerLevel: return (txtCmdTrigLevelSet?.Text ?? string.Empty).Trim();
                case ScopeCommand.QueryTimeDiv: return (txtCmdTimeDivQ?.Text ?? string.Empty).Trim();
                case ScopeCommand.SetTimeDiv: return (txtCmdTimeDivSet?.Text ?? string.Empty).Trim();
                case ScopeCommand.DumpImage: return (txtCmdDumpImage?.Text ?? string.Empty).Trim();
                default: return string.Empty;
            }
        }

        private string GetDefaultScpiForCurrentProfile(ScopeCommand cmd)
        {
            // 0) Prefer the current UI textbox value (blank means "intentionally skip")
            var ui = ResolveUiScpiFor(cmd);
            // Always use UI when available; it’s what the user sees/edits
            if (ui != null) return ui;

            // 1) Per-profile override from config (including empty => skip)
            var prof = FindCurrentProfileOverride();
            if (prof != null && prof.Overrides != null)
            {
                var ov = prof.Overrides.FirstOrDefault(o =>
                    string.Equals(o.Command, cmd.ToString(), StringComparison.OrdinalIgnoreCase));
                if (ov != null)
                    return ov.Value ?? string.Empty;
            }

            // 2) Fallback to profile default
            var vendor = cboVendor.SelectedItem as string;
            var modelDisplay = cboModel.SelectedItem as string;
            var model = ToModelPattern(modelDisplay);

            string scpi;
            var profSpecific = ScpiProfileRegistry.Find(vendor, model);
            if (profSpecific != null && profSpecific.TryGet(cmd, out scpi)) return scpi;

            return string.Empty;
        }

        private void ApplyScpiOverridesForCurrentProfile()
        {
            var vendor = cboVendor.SelectedItem as string ?? string.Empty;
            var modelDisplay = cboModel.SelectedItem as string ?? string.Empty;
            var model = ToModelPattern(modelDisplay);
            var prof = _config.ScpiProfiles.FirstOrDefault(p => string.Equals(p.Vendor, vendor, StringComparison.OrdinalIgnoreCase) && string.Equals(p.Model, model, StringComparison.OrdinalIgnoreCase));
            if (prof == null) return;

            Action<TextBox, string> apply = (tb, cmdName) =>
            {
                if (tb == null) return;
                var ov = prof.Overrides.FirstOrDefault(o => string.Equals(o.Command, cmdName, StringComparison.OrdinalIgnoreCase));
                if (ov != null)
                {
                    // Apply override even if empty -> enables intentional "skip"
                    tb.Text = ov.Value ?? string.Empty;
                }
            };

            apply(txtCmdIdentify, nameof(ScopeCommand.Identify));
            apply(txtCmdClearStats, nameof(ScopeCommand.ClearStatistics));
            apply(txtCmdSysErr, nameof(ScopeCommand.DrainSystemErrorQueue));
            apply(txtCmdOpc, nameof(ScopeCommand.OperationComplete));
            apply(txtCmdActiveTrig, nameof(ScopeCommand.QueryActiveTrigger));
            apply(txtCmdStop, nameof(ScopeCommand.Stop));
            apply(txtCmdSingle, nameof(ScopeCommand.Single));
            apply(txtCmdRun, nameof(ScopeCommand.Run));
            apply(txtCmdTrigMode, nameof(ScopeCommand.QueryTriggerMode));
            apply(txtCmdTrigLevelQ, nameof(ScopeCommand.QueryTriggerLevel));
            apply(txtCmdTrigLevelSet, nameof(ScopeCommand.SetTriggerLevel));
            apply(txtCmdTimeDivQ, nameof(ScopeCommand.QueryTimeDiv));
            apply(txtCmdTimeDivSet, nameof(ScopeCommand.SetTimeDiv));
            apply(txtCmdDumpImage, nameof(ScopeCommand.DumpImage));
        }

        private void ShowCenterOverlay(string text, int milliseconds = 1700)
        {
            if (picScreen == null) return;

            if (_deleteOverlayLabel == null)
            {
                _deleteOverlayLabel = new Label
                {
                    AutoSize = true,
                    BackColor = Color.IndianRed,
                    ForeColor = Color.White,
                    Padding = new Padding(12, 8, 12, 8),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                try { _deleteOverlayLabel.Font = new Font(Font.FontFamily, 20f, FontStyle.Bold); } catch { }
                // put the label directly on top of the image
                _deleteOverlayLabel.Parent = picScreen;
            }

            _deleteOverlayLabel.Text = text ?? string.Empty;
            _deleteOverlayLabel.Visible = true;
            _deleteOverlayLabel.BringToFront();

            // center within picScreen
            var x = Math.Max(0, (picScreen.ClientSize.Width - _deleteOverlayLabel.Width) / 2);
            var y = Math.Max(0, (picScreen.ClientSize.Height - _deleteOverlayLabel.Height) / 2);
            _deleteOverlayLabel.Location = new Point(x, y);

            if (_deleteOverlayTimer == null)
            {
                _deleteOverlayTimer = new System.Windows.Forms.Timer();
                _deleteOverlayTimer.Tick += (s, a) =>
                {
                    _deleteOverlayTimer.Stop();
                    if (_deleteOverlayLabel != null) _deleteOverlayLabel.Visible = false;
                };
            }
            _deleteOverlayTimer.Interval = Math.Max(200, milliseconds);
            _deleteOverlayTimer.Stop();
            _deleteOverlayTimer.Start();
        }

        private void ShowWaitOverlay(string text = "Wait ...")
        {
            try
            {
                if (picScreen == null) return;

                if (_deleteOverlayLabel == null)
                {
                    _deleteOverlayLabel = new Label
                    {
                        AutoSize = true,
                        BackColor = Color.IndianRed,
                        ForeColor = Color.White,
                        Padding = new Padding(12, 8, 12, 8),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    try { _deleteOverlayLabel.Font = new Font(Font.FontFamily, 20f, FontStyle.Bold); } catch { }
                    _deleteOverlayLabel.Parent = picScreen;
                }

                // Cancel any pending auto-hide for the delete overlay
                try { _deleteOverlayTimer?.Stop(); } catch { }

                _deleteOverlayLabel.Text = text ?? string.Empty;
                _deleteOverlayLabel.Visible = true;
                _deleteOverlayLabel.BringToFront();

                // center within picScreen
                var x = Math.Max(0, (picScreen.ClientSize.Width - _deleteOverlayLabel.Width) / 2);
                var y = Math.Max(0, (picScreen.ClientSize.Height - _deleteOverlayLabel.Height) / 2);
                _deleteOverlayLabel.Location = new Point(x, y);
            }
            catch { /* best-effort */ }
        }

        private void HideWaitOverlay()
        {
            try
            {
                try { _deleteOverlayTimer?.Stop(); } catch { }
                if (_deleteOverlayLabel != null) _deleteOverlayLabel.Visible = false;
            }
            catch { /* best-effort */ }
        }

        private void PopulateCommandTextboxes()
        {
            var vendor = cboVendor.SelectedItem as string;
            var modelDisplay = cboModel.SelectedItem as string;
            var model = ToModelPattern(modelDisplay);
            var profSpecific = ScpiProfileRegistry.Find(vendor, model);

            string GetCmd(ScopeCommand cmd)
            {
                string scpi;
                if (profSpecific != null && profSpecific.TryGet(cmd, out scpi)) return scpi;
                return string.Empty;
            }

            txtCmdIdentify.Text = GetCmd(ScopeCommand.Identify);
            txtCmdClearStats.Text = GetCmd(ScopeCommand.ClearStatistics);
            txtCmdActiveTrig.Text = GetCmd(ScopeCommand.QueryActiveTrigger);
            txtCmdStop.Text = GetCmd(ScopeCommand.Stop);
            txtCmdRun.Text = GetCmd(ScopeCommand.Run);
            txtCmdSingle.Text = GetCmd(ScopeCommand.Single);
            txtCmdTrigMode.Text = GetCmd(ScopeCommand.QueryTriggerMode);
            txtCmdTrigLevelQ.Text = GetCmd(ScopeCommand.QueryTriggerLevel);
            txtCmdTrigLevelSet.Text = GetCmd(ScopeCommand.SetTriggerLevel);
            txtCmdTimeDivQ.Text = GetCmd(ScopeCommand.QueryTimeDiv);
            txtCmdTimeDivSet.Text = GetCmd(ScopeCommand.SetTimeDiv);
            txtCmdDumpImage.Text = GetCmd(ScopeCommand.DumpImage);
            txtCmdSysErr.Text = GetCmd(ScopeCommand.DrainSystemErrorQueue);
            txtCmdOpc.Text = GetCmd(ScopeCommand.OperationComplete);
        }

        // REPLACE: show effective values, preferring saved override if present
        private void PopulateTimeDivTextbox()
        {
            try
            {
                var tb = this.Controls.Find("txtTimeDivValues", true).FirstOrDefault() as TextBox;
                if (tb == null) return;

                var vendor = cboVendor.SelectedItem as string;
                var modelDisplay = cboModel.SelectedItem as string;
                var model = ToModelPattern(modelDisplay);

                if (string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(model))
                {
                    tb.Text = string.Empty;
                    return;
                }

                // Seed runtime override from config, if present for this exact vendor+model
                var prof = _config?.ScpiProfiles?.FirstOrDefault(p =>
                    string.Equals(p.Vendor, vendor, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.Model, model, StringComparison.OrdinalIgnoreCase));

                if (prof != null && !string.IsNullOrWhiteSpace(prof.TimeDivValues))
                {
                    ScpiProfileRegistry.SetTimeDivTextOverride(vendor, model, prof.TimeDivValues);
                }

                var tokens = ScpiProfileRegistry.GetTimeDivTextValues(vendor, model);
                tb.Text = string.Join(", ", tokens ?? Array.Empty<string>());
            }
            catch
            {
                // best-effort
            }
        }

        // NEW: call once in Main_Load after controls are created (and after _config is loaded)
        private void WireTimeDivTextbox()
        {
            var tb = this.Controls.Find("txtTimeDivValues", true).FirstOrDefault() as TextBox;
            if (tb == null) return;

            // Apply on focus loss
            tb.Leave += (s, a) => ApplyTimeDivTextboxToOverrides(tb.Text);

            // Optional: debounce save on edit (keeps UI responsive)
            var saveTimer = new System.Windows.Forms.Timer { Interval = 500 };
            saveTimer.Tick += (s, a) =>
            {
                saveTimer.Stop();
                ApplyTimeDivTextboxToOverrides(tb.Text);
            };
            tb.TextChanged += (s, a) =>
            {
                saveTimer.Stop();
                saveTimer.Start();
            };
        }

        // NEW: parse textbox, apply to ScpiProfileRegistry, and persist in config for current vendor+model
        private void ApplyTimeDivTextboxToOverrides(string text)
        {
            try
            {
                var vendor = cboVendor.SelectedItem as string;
                var modelDisplay = cboModel.SelectedItem as string;
                var model = ToModelPattern(modelDisplay);

                if (string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(model))
                    return;

                // Apply runtime override
                ScpiProfileRegistry.SetTimeDivTextOverride(vendor, model, text);

                // Persist to config
                var prof = _config?.ScpiProfiles?.FirstOrDefault(p =>
                    string.Equals(p.Vendor, vendor, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.Model, model, StringComparison.OrdinalIgnoreCase));

                if (prof == null)
                {
                    prof = new Oscilloscope_Network_Capture.Core.Configuration.ScpiProfileOverride
                    {
                        Vendor = vendor,
                        Model = model
                    };
                    _config.ScpiProfiles.Add(prof);
                }

                prof.TimeDivValues = text ?? string.Empty;
                Oscilloscope_Network_Capture.Core.Configuration.ConfigurationService.Save(_config);
            }
            catch
            {
                // best-effort
            }
        }

        private void UpdateModelsForVendor()
        {
            var vendor = cboVendor.SelectedItem as string;

            // Collect model series from drivers and profiles (filter out "*" generics)
            var driverModels = ScopeFactory.GetAvailableScopes()
                .Where(d => d.Vendor.Equals(vendor ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Model);

            var profileModels = ScpiProfileRegistry.Profiles
                .Where(p => p.Vendor.Equals(vendor ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.ModelPattern);

            var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in driverModels)
                if (!string.IsNullOrWhiteSpace(m) && m.Trim() != "*")
                    models.Add(m.Trim());

            foreach (var m in profileModels)
                if (!string.IsNullOrWhiteSpace(m) && m.Trim() != "*")
                    models.Add(m.Trim());

            var ordered = models
                .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                .ToList();

            cboModel.Items.Clear();
            cboModel.Items.AddRange(ordered.Cast<object>().ToArray());

            // Try to restore the previously saved series for this vendor
            var savedModelDisplay = (_config?.Model ?? string.Empty);
            // If older configs stored "*", that won’t match any series; we’ll fall back to first item.
            int idx = (!string.IsNullOrWhiteSpace(savedModelDisplay))
                ? ordered.FindIndex(x => string.Equals(x, savedModelDisplay, StringComparison.OrdinalIgnoreCase))
                : -1;

            if (idx < 0 && ordered.Count > 0) idx = 0;

            if (idx >= 0)
            {
                // Update selection (this will also raise SelectedIndexChanged)
                cboModel.SelectedIndex = idx;

                // Persist immediately as well (in case the event does not fire in some scenarios)
                var selected = cboModel.SelectedItem as string ?? string.Empty;
                _config.Model = selected;
                Oscilloscope_Network_Capture.Core.Configuration.ConfigurationService.Save(_config);
            }
            else
            {
                // No model available for this vendor; clear persisted model
                _config.Model = string.Empty;
                Oscilloscope_Network_Capture.Core.Configuration.ConfigurationService.Save(_config);
            }
        }

        private int GetDefaultPortForVendor(string vendor)
        {
            if (string.IsNullOrEmpty(vendor)) return 5025;
            switch (vendor.Trim())
            {
                case "Rigol": return 5555;
//                case "Siglent": return 5025;
//                case "Keysight": return 5025;
//                case "Agilent": return 5025;
//                case "Rohde & Schwarz": return 5025;
                default: return 5025;
            }
        }

        private void ApplyVendorDefaultPort()
        {
            var vendor = cboVendor.SelectedItem as string;
            var defPort = GetDefaultPortForVendor(vendor);
            if (defPort >= numPort.Minimum && defPort <= numPort.Maximum)
            {
                numPort.Value = defPort;
            }
        }

        // In btnConnect_Click, auto-start capture mode after a successful ConnectAndRefreshAsync
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            var btn = sender as Button ?? btnConnect;
            await RunConnectivityCheckAsync(btn);
        }

        // Save per-profile override at time of testing — only if different from default
        private void SaveOverrideOnTestClick(ScopeCommand cmd, string value)
        {
            // Compare against raw profile default, not the effective UI value
            var defaultScpi = (GetProfileDefaultScpiForCurrentModel(cmd) ?? string.Empty).Trim();
            var current = (value ?? string.Empty).Trim();

            var prof = FindCurrentProfileOverride();
            var existing = prof?.Overrides?.FirstOrDefault(o => string.Equals(o.Command, cmd.ToString(), StringComparison.OrdinalIgnoreCase));

            // If equals profile default -> remove override
            if (string.Equals(current, defaultScpi, StringComparison.OrdinalIgnoreCase))
            {
                if (existing != null)
                {
                    prof.Overrides.Remove(existing);
                }
                if (prof != null && (prof.Overrides == null || prof.Overrides.Count == 0) && string.IsNullOrWhiteSpace(prof.TimeDivValues))
                {
                    _config.ScpiProfiles.Remove(prof);
                }
                ConfigurationService.Save(_config);
                return;
            }

            // Otherwise persist override AS-IS (including empty => intentional "skip")
            if (prof == null)
            {
                var vendor = cboVendor.SelectedItem as string ?? string.Empty;
                var modelDisplay = cboModel.SelectedItem as string ?? string.Empty;
                var model = ToModelPattern(modelDisplay);
                prof = new ScpiProfileOverride { Vendor = vendor, Model = model, Overrides = new List<ScpiCommandOverride>() };
                _config.ScpiProfiles.Add(prof);
            }
            if (existing == null)
            {
                prof.Overrides.Add(new ScpiCommandOverride { Command = cmd.ToString(), Value = current });
            }
            else
            {
                existing.Value = current;
            }
            ConfigurationService.Save(_config);
        }

        private async void btnTestIdentify_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.Identify, txtCmdIdentify.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusIdentify, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    await RunSuiteAsync(ScopeTestSuite.QueryIdentify, txtCmdIdentify.Text, lblStatusIdentify);
                });
            });
        }

        private async void btnTestClearStats_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.ClearStatistics, txtCmdClearStats.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusClearStats, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    await RunSuiteAsync(ScopeTestSuite.ClearStatistics, txtCmdClearStats.Text, lblStatusClearStats);
                });
            });
        }

        private async void btnTestActiveTrig_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.QueryActiveTrigger, txtCmdActiveTrig.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusActiveTrig, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    await RunSuiteAsync(ScopeTestSuite.QueryActiveTrigger, txtCmdActiveTrig.Text, lblStatusActiveTrig);
                });
            });
        }

        private async void btnTestStop_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.Stop, txtCmdStop.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusStop, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    await RunSuiteAsync(ScopeTestSuite.Stop, txtCmdStop.Text, lblStatusStop);
                });
            });
        }

        private async void btnTestRun_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.Run, txtCmdRun.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusRun, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    await RunSuiteAsync(ScopeTestSuite.Run, txtCmdRun.Text, lblStatusRun);
                });
            });
        }

        private async void btnTestSingle_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.Single, txtCmdSingle.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusSingle, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    await RunSuiteAsync(ScopeTestSuite.Single, txtCmdSingle.Text, lblStatusSingle);
                });
            });
        }

        private async void btnTestTrigMode_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.QueryTriggerMode, txtCmdTrigMode.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusTrigMode, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    await RunSuiteAsync(ScopeTestSuite.QueryTriggerMode, txtCmdTrigMode.Text, lblStatusTrigMode);
                });
            });
        }

        private async void btnTestTrigLevelQ_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.QueryTriggerLevel, txtCmdTrigLevelQ.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusTrigLevelQ, async () =>
            {
                double current = 0;
                bool success = false;

                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    try
                    {
                        await EnsureConnectedAsync();
                        current = await QueryTriggerLevelViaSuiteAsync().ConfigureAwait(true);

                        lblStatusTrigLevelQ.Text = "OK";
                        lblStatusTrigLevelQ.ForeColor = Color.Green;
                        success = true;
                    }
                    catch
                    {
                        lblStatusTrigLevelQ.Text = "Fail";
                        lblStatusTrigLevelQ.ForeColor = Color.Red;
                    }
                });

                // Apply final Set-button state after other buttons have been restored
                if (btnTestTrigLevelSet != null)
                {
                    if (success)
                    {
                        btnTestTrigLevelSet.Tag = current;
                        btnTestTrigLevelSet.Enabled = true;
                        if (lblStatusTrigLevelSet.Text == "Not tested - first run \"Query trigger level\"")
                        {
                            lblStatusTrigLevelSet.Text = "Not tested";
                        }
                    }
                    else
                    {
                        btnTestTrigLevelSet.Tag = null;
                        btnTestTrigLevelSet.Enabled = false;
                    }
                }
            });
        }

        private async void btnTestTrigLevelSet_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.SetTriggerLevel, txtCmdTrigLevelSet.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusTrigLevelSet, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    try
                    {
                        await EnsureConnectedAsync();
                        var ct = _cts?.Token ?? CancellationToken.None;

                        double target;
                        if (btnTestTrigLevelSet != null && btnTestTrigLevelSet.Tag is double d)
                            target = d;
                        else
                            target = await _scope.QueryTriggerLevelAsync(ct).ConfigureAwait(true);

                        await RunSetTriggerLevelSuiteAsync(target).ConfigureAwait(true);

                        lblStatusTrigLevelSet.Text = "OK";
                        lblStatusTrigLevelSet.ForeColor = Color.Green;
                    }
                    catch
                    {
                        lblStatusTrigLevelSet.Text = "Fail";
                        lblStatusTrigLevelSet.ForeColor = Color.Red;
                    }
                });
            });
        }

        private async void btnTestTimeDivQ_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.QueryTimeDiv, txtCmdTimeDivQ.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusTimeDivQ, async () =>
            {
                double seconds = 0;
                bool success = false;

                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    try
                    {
                        await EnsureConnectedAsync();
                        seconds = await QueryTimeDivViaSuiteAsync().ConfigureAwait(true);

                        lblStatusTimeDivQ.Text = "OK";
                        lblStatusTimeDivQ.ForeColor = Color.Green;
                        success = true;
                    }
                    catch
                    {
                        lblStatusTimeDivQ.Text = "Fail";
                        lblStatusTimeDivQ.ForeColor = Color.Red;
                    }
                });

                if (btnTestTimeDivSet != null)
                {
                    if (success)
                    {
                        btnTestTimeDivSet.Tag = seconds;
                        btnTestTimeDivSet.Enabled = true;
                        if (lblStatusTimeDivSet.Text == "Not tested - first run \"Query TIME/DIV\"")
                        {
                            lblStatusTimeDivSet.Text = "Not tested";
                        }
                    }
                    else
                    {
                        btnTestTimeDivSet.Tag = null;
                        btnTestTimeDivSet.Enabled = false;
                    }
                }
            });
        }

        private async void btnTestTimeDivSet_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.SetTimeDiv, txtCmdTimeDivSet.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusTimeDivSet, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    try
                    {
                        await EnsureConnectedAsync();

                        double targetSeconds;
                        if (btnTestTimeDivSet != null && btnTestTimeDivSet.Tag is double s2)
                            targetSeconds = s2;
                        else
                            targetSeconds = await QueryTimeDivViaSuiteAsync().ConfigureAwait(true);

                        await RunSetTimeDivSuiteAsync(targetSeconds).ConfigureAwait(true);

                        lblStatusTimeDivSet.Text = "OK";
                        lblStatusTimeDivSet.ForeColor = Color.Green;
                    }
                    catch
                    {
                        lblStatusTimeDivSet.Text = "Fail";
                        lblStatusTimeDivSet.ForeColor = Color.Red;
                    }
                });
            });
        }

        private async void btnTestSysErr_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.DrainSystemErrorQueue, txtCmdSysErr.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusSysErr, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    if ((ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        await DrainSystemErrorQueueAsync();
                        if (lblStatusSysErr != null) { lblStatusSysErr.Text = "OK"; lblStatusSysErr.ForeColor = Color.Green; }
                    }
                    else
                    {
                        await RunSuiteAsync(ScopeTestSuite.DrainSystemErrorQueue, txtCmdSysErr.Text, lblStatusSysErr);
                    }
                });
            });
        }

        private async void btnTestOpc_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.OperationComplete, txtCmdOpc.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusOpc, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    await RunSuiteAsync(ScopeTestSuite.OperationComplete, txtCmdOpc.Text, lblStatusOpc);
                });
            });
        }

        private async void btnTestDumpImage_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.DumpImage, txtCmdDumpImage.Text);

            await WithButtonDisabledAsync((Button)sender, lblStatusDumpImage, async () =>
            {
                await WithOtherTestButtonsDisabledAsync((Button)sender, async () =>
                {
                    try
                    {
                        await EnsureConnectedAsync();

                        // Honor "Force Clear" with optional delay before dumping
                        if (_config != null && _config.ForceClear)
                        {
                            try
                            {
                                await RunSuiteHeadlessAsync(ScopeTestSuite.ClearStatistics);
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Error("ClearStatistics suite failed before dump: " + InnermostMessage(ex));
                            }

                            try
                            {
                                var delayMs = Math.Max(0, _config?.DelayMs ?? 0);
                                if (delayMs > 0)
                                {
                                    Logger.Instance.Debug("---");
                                    Logger.Instance.Debug("Delay before dump: " + delayMs.ToString(CultureInfo.InvariantCulture) + " ms");
                                    await Task.Delay(delayMs, _cts?.Token ?? CancellationToken.None);
                                    Logger.Instance.Debug("Delay completed.");
                                }
                            }
                            catch (TaskCanceledException) { }
                            catch (Exception ex)
                            {
                                Logger.Instance.Error("Delay after ClearStatistics failed: " + InnermostMessage(ex));
                            }
                        }

                        // Honor Stop-before-dump
                        try
                        {
                            await RunSuiteHeadlessAsync(ScopeTestSuite.Stop);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error("Stop suite before dump failed: " + InnermostMessage(ex));
                        }

                        Logger.Instance.Debug("---");
                        Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.DumpImage) + ":");

                        // Execute pre-steps (all steps except DumpImage) using effective (UI-first) SCPI
                        var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.DumpImage);
                        foreach (var step in steps)
                        {
                            if (step == ScopeCommand.DumpImage) continue;

                            var scpi = GetDefaultScpiForCurrentProfile(step);
                            if (string.IsNullOrWhiteSpace(scpi))
                            {
                                LogSkipEmpty(step);
                                continue; // Skip empty pre-steps
                            }

                            if (IsQuery(step))
                                await _scope.SendRawQueryAsync(scpi, _cts?.Token ?? CancellationToken.None);
                            else
                                await _scope.SendRawWriteAsync(scpi, _cts?.Token ?? CancellationToken.None);
                        }

                        var cmd = (txtCmdDumpImage.Text ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(cmd))
                            throw new InvalidOperationException("DumpImage command is empty.");

                        var raw = await _scope.SendRawDumpAndReadAsync(cmd, _cts?.Token ?? CancellationToken.None);

                        var payload = StripIeee4882Block(raw);
                        if (payload == null || payload.Length == 0)
                            throw new InvalidOperationException("No image data received.");

                        // Decode must succeed or we fail the test
                        try
                        {
                            using (var ms = new MemoryStream(payload))
                            using (var img = Image.FromStream(ms))
                            {
                                var clone = (Image)img.Clone();
                                var old = picScreen.Image;
                                picScreen.Image = clone;
                                try { old?.Dispose(); } catch { }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error("Image decode failed: " + InnermostMessage(ex));
                            throw; // ensure status becomes Fail
                        }

                        var info = Oscilloscope_Network_Capture.Core.Scopes.ScopeTestSuiteRegistry.FormatDumpImageInfo(raw);
                        Logger.Instance.Info(info);

                        // Honor "Force Acquisition" after dumping
                        if (_config != null && _config.ForceAcquisition)
                        {
                            try
                            {
                                await RunSuiteHeadlessAsync(ScopeTestSuite.Run);
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Error("Run suite after dump failed: " + InnermostMessage(ex));
                            }
                        }

                        lblStatusDumpImage.Text = "OK";
                        lblStatusDumpImage.ForeColor = Color.Green;
                    }
                    catch
                    {
                        lblStatusDumpImage.Text = "Fail";
                        lblStatusDumpImage.ForeColor = Color.Red;
                    }
                });
            });
        }

        private string GetProfileDefaultScpiForCurrentModel(ScopeCommand cmd)
        {
            var vendor = cboVendor.SelectedItem as string;
            var modelDisplay = cboModel.SelectedItem as string;
            var model = ToModelPattern(modelDisplay);

            string scpi;
            var profSpecific = ScpiProfileRegistry.Find(vendor, model);
            return (profSpecific != null && profSpecific.TryGet(cmd, out scpi)) ? (scpi ?? string.Empty) : string.Empty;
        }

        // Helper: inject numeric into SCPI override. If "{0}" exists, format; else replace last number; else append
        private static string InjectNumericArg(string fmt, double value)
        {
            if (string.IsNullOrWhiteSpace(fmt)) return null;

            var valStr = value.ToString("0.######", CultureInfo.InvariantCulture);

            if (fmt.IndexOf("{0}", StringComparison.Ordinal) >= 0)
                return string.Format(CultureInfo.InvariantCulture, fmt, valStr);

            var m = System.Text.RegularExpressions.Regex.Matches(fmt, @"[+-]?(?:\d+\.?\d*|\d*\.?\d+)(?:[eE][+-]?\d+)?");
            if (m.Count > 0)
            {
                var last = m[m.Count - 1];
                return fmt.Substring(0, last.Index) + valStr + fmt.Substring(last.Index + last.Length);
            }

            return (fmt.TrimEnd() + " " + valStr);
        }
        private static byte[] StripIeee4882Block(byte[] raw)
        {
            var data = raw;
            if (data != null && data.Length >= 2 && data[0] == (byte)'#')
            {
                int nDigits = data[1] - (byte)'0';
                if (nDigits >= 0 && nDigits <= 9 && data.Length >= 2 + nDigits)
                {
                    int payloadLen = 0;
                    for (int i = 0; i < nDigits; i++)
                        payloadLen = (payloadLen * 10) + (data[2 + i] - (byte)'0');
                    int headerLen = 2 + nDigits;
                    if (payloadLen >= 0 && data.Length >= headerLen + payloadLen)
                    {
                        var payload = new byte[payloadLen];
                        Buffer.BlockCopy(data, headerLen, payload, 0, payloadLen);
                        data = payload;
                    }
                }
            }
            return data ?? new byte[0];
        }

        private async void buttonSendToDeveloper_Click(object sender, EventArgs e)
        {
            // Optional: ensure modern TLS
            try { System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12; } catch { }

            var btn = sender as Button;
            try
            {
                if (btn != null) btn.Enabled = false;

                // Require non-empty feedback text
                string feedback = string.Empty;
                if (this.Controls.Find("textBoxFeedback", true).FirstOrDefault() is TextBox tbFeedback)
                    feedback = tbFeedback.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(feedback))
                {
                    MessageBox.Show("Please provide some information in the feedback field before sending.", "Feedback",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Gather email (prefer current textbox value; fallback to config)
                string email = _config?.Email ?? string.Empty;
                if (this.Controls.Find("textBoxEmail", true).FirstOrDefault() is TextBox tbEmail)
                    email = tbEmail.Text?.Trim() ?? email;

                if (!Oscilloscope_Network_Capture.Core.Online.Online.IsValidEmail(email))
                {
                    MessageBox.Show("Please enter a valid email address or leave it empty.", "Invalid Email",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Determine attachments via checkboxes (default: include if checkbox missing)
                var cbDebug = this.Controls.Find("checkBoxFeedbackAttachDebug", true).FirstOrDefault() as CheckBox;
                var cbConfig = this.Controls.Find("checkBoxFeedbackAttachConfig", true).FirstOrDefault() as CheckBox;
                bool attachDebug = (cbDebug == null) ? true : cbDebug.Checked;
                bool attachConfig = (cbConfig == null) ? true : cbConfig.Checked;

                // Build payloads based on checkbox state
                var cfgXml = attachConfig
                    ? Oscilloscope_Network_Capture.Core.Online.Online.SerializeConfig(_config)
                    : "(chose not to send configuration file)";

                var debug = attachDebug
                    ? Oscilloscope_Network_Capture.Core.Online.Online.ReadDebugLog()
                    : "(chose not to send debug log)";

                Logger.Instance.Info("Sending feedback to developer.");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(35)))
                {
                    var resp = await Oscilloscope_Network_Capture.Core.Online.Online
                        .SendFeedbackAsync(cfgXml, debug, email, versionThis, feedback, cts.Token)
                        .ConfigureAwait(true);

                    if (string.Equals(resp, "Success", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Instance.Info("Feedback sent successfully.");
                        MessageBox.Show("Thank you! Feedback sent successfully.", "Feedback",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        Logger.Instance.Error("Feedback service returned: " + (resp ?? string.Empty));
                        MessageBox.Show("Server response: " + (resp ?? "(empty)"), "Feedback",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Logger.Instance.Error("Feedback send timed out.");
                MessageBox.Show("Timed out while contacting the server.", "Feedback",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Failed to send feedback: " + InnermostMessage(ex));
                MessageBox.Show("Failed to send feedback: " + InnermostMessage(ex), "Feedback",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (btn != null) btn.Enabled = true;
            }
        }

        private static string FriendlyStepName(ScopeCommand cmd)
        {
            switch (cmd)
            {
                case ScopeCommand.DrainSystemErrorQueue: return "\"System Error Drain\"";
                case ScopeCommand.OperationComplete: return "\"Operation Complete\"";
                case ScopeCommand.ClearStatistics: return "\"Clear Statistics\"";
                case ScopeCommand.QueryActiveTrigger: return "\"Query Active Trigger\"";
                case ScopeCommand.Stop: return "\"Stop\"";
                case ScopeCommand.Single: return "\"Single\"";
                case ScopeCommand.Run: return "\"Run\"";
                case ScopeCommand.QueryTriggerMode: return "\"Query Trigger Mode\"";
                case ScopeCommand.QueryTriggerLevel: return "\"Query Trigger Level\"";
                case ScopeCommand.SetTriggerLevel: return "\"Set Trigger Level\"";
                case ScopeCommand.QueryTimeDiv: return "\"Query TIME/DIV\"";
                case ScopeCommand.SetTimeDiv: return "\"Set TIME/DIV\"";
                case ScopeCommand.Identify: return "\"Identify\"";
                case ScopeCommand.DumpImage: return "\"Dump Image\"";
                default: return cmd.ToString();
            }
        }

        private static void LogSkipEmpty(ScopeCommand step)
        {
            Logger.Instance.Debug("Skipping " + FriendlyStepName(step) + " (command empty).");
        }

        private async Task RunSuiteAsync(ScopeTestSuite suite, string primaryScpi, Label statusLabel)
        {
            try
            {
                await EnsureConnectedAsync();
                var steps = ScopeTestSuiteRegistry.Resolve(_config, suite);
                string lastQueryResponse = null;
                bool suiteFailed = false;

                Logger.Instance.Debug("---");
                Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(suite) + ":");

                foreach (var step in steps)
                {
                    if (step == ScopeCommand.DrainSystemErrorQueue)
                    {
                        await DrainSystemErrorQueueAsync().ConfigureAwait(true);
                        lastQueryResponse = "0";
                        continue;
                    }

                    if (step == steps.First())
                    {
                        var cmdToSend = (primaryScpi ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(cmdToSend))
                        {
                            LogSkipEmpty(step);
                            continue;
                        }

                        if (IsQuery(step))
                            lastQueryResponse = await _scope.SendRawQueryAsync(cmdToSend, _cts?.Token ?? CancellationToken.None);
                        else if (step != ScopeCommand.DumpImage)
                            await _scope.SendRawWriteAsync(cmdToSend, _cts?.Token ?? CancellationToken.None);
                    }
                    else
                    {
                        if (step == ScopeCommand.DumpImage)
                            continue;

                        var scpi = GetDefaultScpiForCurrentProfile(step);
                        if (string.IsNullOrWhiteSpace(scpi))
                        {
                            LogSkipEmpty(step);
                            continue; // Skip empty profile step
                        }

                        if (IsQuery(step))
                            lastQueryResponse = await _scope.SendRawQueryAsync(scpi, _cts?.Token ?? CancellationToken.None);
                        else
                            await _scope.SendRawWriteAsync(scpi, _cts?.Token ?? CancellationToken.None);
                    }
                }

                if (!suiteFailed && suite == ScopeTestSuite.QueryIdentify && !string.IsNullOrWhiteSpace(lastQueryResponse))
                {
                    LogIdnVendorModelFirmware(lastQueryResponse);
                }

                if (suiteFailed)
                {
                    statusLabel.Text = "Fail";
                    statusLabel.ForeColor = Color.Red;
                }
                else
                {
                    statusLabel.Text = "OK";
                    statusLabel.ForeColor = Color.Green;
                }
            }
            catch (Exception)
            {
                statusLabel.Text = "Fail";
                statusLabel.ForeColor = Color.Red;
            }
        }

        private static bool IsQuery(ScopeCommand cmd)
        {
            switch (cmd)
            {
                case ScopeCommand.Identify:
                case ScopeCommand.QueryActiveTrigger:
                case ScopeCommand.QueryTriggerMode:
                case ScopeCommand.QueryTriggerLevel:
                case ScopeCommand.QueryTimeDiv:
                case ScopeCommand.DrainSystemErrorQueue:
                case ScopeCommand.OperationComplete:
                    return true;
                default:
                    return false;
            }
        }

        private void UpdateScpiHeaderLabel()
        {
            try
            {
                var lbl = this.Controls.Find("label11", true).FirstOrDefault() as Label;
                if (lbl == null) return;
                var vendor = cboVendor.SelectedItem as string ?? (_config?.Vendor ?? string.Empty);
                var modelDisplay = cboModel.SelectedItem as string ?? ToDisplayModel(_config?.Model ?? "*");
                if (string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(modelDisplay)) return;
                lbl.Text = string.Format("Test-suites for SCPI commands specifically for {0} {1}", vendor, modelDisplay);
            }
            catch { }
        }

        private void PlaceLabelTopRight(Label lbl, int paddingRight = 8)
        {
            if (lbl == null) return;
            var parent = lbl.Parent; if (parent == null) return;
            int x = parent.ClientSize.Width - lbl.Width;
            lbl.Location = new System.Drawing.Point(x, 0);
        }

        private void ApplyTopRightBadges()
        {
            // Applies to all known version labels if they exist
            string[] names = new[] { "labelNewVersion1", "labelNewVersion2", "labelNewVersion3", "labelNewVersion4", "labelNewVersion5", "labelNewVersion6" };
            foreach (var n in names)
            {
                var lbl = this.Controls.Find(n, true).FirstOrDefault() as Label;
                if (lbl != null) PlaceLabelTopRight(lbl, 8);
            }
        }

        private void CheckForNewVersionAsync()
        {
            try { System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12; } catch { }

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
                {

                    var newestTask = Oscilloscope_Network_Capture.Core.Online.Online.GetNewestVersionAsync(cts.Token);
                    versionOnline = (newestTask != null ? newestTask.GetAwaiter().GetResult() : string.Empty).Trim();

                    if (!string.IsNullOrWhiteSpace(versionOnline) &&
                        !string.Equals(versionOnline, versionThis, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Instance.Debug("---");
                        Logger.Instance.Info("Newer version is available: " + versionOnline + " - view \"About\" tab for where to download it.");
                        ShowNewVersionBadge(versionOnline);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Version check failed: " + InnermostMessage(ex));
            }
        }

        private void ShowNewVersionBadge(string newest)
        {
//            string text = "New version: " + newest;
            string[] names = new[] { "labelNewVersion1", "labelNewVersion2", "labelNewVersion3", "labelNewVersion4", "labelNewVersion5", "labelNewVersion6" };
            foreach (var n in names)
            {
                var lbl = this.Controls.Find(n, true).FirstOrDefault() as Label;
                if (lbl != null)
                {
//                    lbl.Text = text;
                    lbl.Visible = true;
                    try { PlaceLabelTopRight(lbl, 8); } catch { }
                }
            }
        }

        private void UpdateActionRichTextForNextFilename()
        {
            var rtb = this.Controls.Find("richTextBoxAction", true).FirstOrDefault() as RichTextBox;
            if (rtb == null) return;

            // Do not modify the text unless capture mode is active
            if (!_captureMode) return;

            // Build exact next filename (same as saving)
            var baseName = BuildFileNameFromFormat();
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "capture";
            baseName = SanitizeFileName(baseName);
            var nextFileName = baseName + ".png";

            rtb.Clear();
            rtb.AppendText("Capture mode enabled. Press [ENTER] to capture image. View \"Help\" tab to see all keyboard commands available." + Environment.NewLine);

            // PREVIOUS filename, if available
            if (_savedFileHistory.Count > 0)
            {
                var prev = _savedFileHistory[_savedFileHistory.Count - 1];
                rtb.AppendText("Previous image was saved with filename [");

                int prevBoldStart = rtb.TextLength;
                rtb.AppendText(prev);
                int prevBoldLen = rtb.TextLength - prevBoldStart;

                rtb.SelectionStart = prevBoldStart;
                rtb.SelectionLength = prevBoldLen;
                try { rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold); } catch { }

                // Reset to regular before the closing bracket and any subsequent text
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                try { rtb.SelectionFont = rtb.Font; } catch { }

                rtb.AppendText("]" + Environment.NewLine);
            }

            // NEXT filename
            rtb.AppendText("Next image will be saved with filename [");

            // Bold NEXT filename only
            int boldStart = rtb.TextLength;
            rtb.AppendText(nextFileName);
            int boldLen = rtb.TextLength - boldStart;

            rtb.SelectionStart = boldStart;
            rtb.SelectionLength = boldLen;
            try { rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold); } catch { }

            // Reset insertion font to regular so following text is NOT bold
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            try { rtb.SelectionFont = rtb.Font; } catch { }

            rtb.AppendText("]");
        }

        private void IncrementNumberAfterSuccessfulSave()
        {
            try
            {
                if (numericUpDown1 == null) return;
                var next = numericUpDown1.Value + 1;
                if (next <= numericUpDown1.Maximum)
                {
                    numericUpDown1.Value = next; // triggers ValueChanged -> saves + updates preview
                }
                else
                {
                    Logger.Instance.Info("NUMBER is at maximum; not incremented.");
                }
            }
            catch { }
        }

        // Decrement NUMBER after a successful delete
        private void DecrementNumberAfterDelete()
        {
            try
            {
                if (numericUpDown1 == null) return;
                var prev = numericUpDown1.Value - 1;
                if (prev >= numericUpDown1.Minimum)
                {
                    numericUpDown1.Value = prev; // triggers ValueChanged -> saves + updates preview
                }
                else
                {
                    Logger.Instance.Info("NUMBER is at minimum; not decremented.");
                }
            }
            catch { }
        }

        // Delete the most recently saved file (by history), update NUMBER and UI
        private void DeleteLastSavedFile()
        {
            if (!_captureMode) return;

            if (_savedFileHistory.Count == 0)
            {
                Logger.Instance.Info("No previous images to delete.");
                return;
            }

            var lastFileName = _savedFileHistory[_savedFileHistory.Count - 1];
            var folder = ResolveCaptureFolder();
            var path = Path.Combine(folder, lastFileName);

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Logger.Instance.Info("Deleted capture: " + path);
                }
                else
                {
                    // Treat as already deleted; keep history consistent
                    Logger.Instance.Info("Last capture not found on disk (already deleted?): " + path);
                }

                // Pop from history, decrement NUMBER, refresh text
                _savedFileHistory.RemoveAt(_savedFileHistory.Count - 1);
                DecrementNumberAfterDelete();
                UpdateActionRichTextForNextFilename();

                // Show a brief overlay if we're on the Measurements tab
                try
                {
                    if (tabMain != null && tabCapturing != null && tabMain.SelectedTab == tabCapturing)
                        ShowCenterOverlay("Deleted last saved file");
                }
                catch { }

                // NEW: subtle delete beep (respects the same EnableBeep setting)
                if (_config != null && _config.EnableBeep) BeepDelete();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Delete failed: " + InnermostMessage(ex));
                MessageBox.Show("Unable to delete file: " + InnermostMessage(ex), "Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Capture mode: ENTER=stop+capture+save, ESC=exit
        private async Task StartCaptureModeAsync()
        {
            try
            {
                await EnsureConnectedAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Unable to start capture mode: " + InnermostMessage(ex));
                MessageBox.Show("Unable to start capture mode: " + InnermostMessage(ex), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _captureMode = true;
            this.KeyPreview = true;
            this.KeyDown -= Form1_Capture_KeyDown;
            this.KeyDown += Form1_Capture_KeyDown;
            Logger.Instance.Info("Capture mode enabled.");

            // Update indicators to show capture-mode is active
            UpdateCaptureModeIndicators();

            UpdateActionRichTextForNextFilename();

            // Begin monitoring the connection (only while capture mode is active)
            EnsureConnectionMonitorStarted();
        }

        // Add this helper near other private helpers (e.g., below StartCaptureModeAsync / ExitCaptureMode)
        private async Task AutoStartCaptureModeIfConnectedAsync()
        {
            try
            {
                if (IsDisposed) return;
                if (_captureMode) return; // already active

                // Only trust our own connectable flag (set after successful checks), not the raw socket flag.
                if (_isConnectable && _scope != null && _scope.IsConnected)
                {
                    Logger.Instance.Info("Auto-starting capture mode.");
                    await StartCaptureModeAsync().ConfigureAwait(true);
                }
            }
            catch
            {
                // best-effort
            }
        }

        // Update the capture-mode key handler to route scope-affecting actions through the busy gate
        // Update the capture-mode key handler to enforce the allowed tabs
        private void Form1_Capture_KeyDown(object sender, KeyEventArgs e)
        {
            // Only process keys while capture mode is active
            if (!_captureMode) return;

            // Only allow hotkeys when on Capturing or Debug tabs
            if (!IsKeyboardContextAllowed()) return;

            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                FlashEnterFeedback(); // subtle visual feedback
                _ = ExecuteCaptureKeyAsync(async () =>
                {
                    await CaptureAndSaveAsync();
                });
                return;
            }

            if (e.KeyCode == Keys.Delete)
            {
                if (checkBoxEnableDelete.Checked)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DeleteLastSavedFile();
                }
                return;
            }

            // NEW: Numpad 9 => Set TIME/DIV to 1 µs
            if (e.KeyCode == Keys.NumPad9)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = ExecuteCaptureKeyAsync(async () =>
                {
                    await RunSetTimeDivSuiteAsync(1e-6); // 1 microsecond
                });
                return;
            }

            // * => SINGLE (Numpad * or Shift+8)
            if (e.KeyCode == Keys.Multiply || (e.KeyCode == Keys.D8 && e.Shift))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = ExecuteCaptureKeyAsync(async () => { await RunSuiteHeadlessAsync(ScopeTestSuite.Single); });
                return;
            }

            // / => RUN (Numpad / or / key)
            if (e.KeyCode == Keys.Divide || e.KeyCode == Keys.OemQuestion)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = ExecuteCaptureKeyAsync(async () => { await RunSuiteHeadlessAsync(ScopeTestSuite.Run); });
                return;
            }

            // UP => adjust trigger up
            if (e.KeyCode == Keys.Up)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                _ = ExecuteCaptureKeyAsync(
                    async () =>
                    {
                        AdjustTriggerLevelAsync(+1);
                        var worker = _triggerAdjustWorker;
                        if (worker != null) await worker.ConfigureAwait(true);
                    },
                    KeyActionType.AdjustTrigger,
                    coalesceIfBusy: () => AdjustTriggerLevelAsync(+1)
                );
                return;
            }

            // DOWN => adjust trigger down
            if (e.KeyCode == Keys.Down)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                _ = ExecuteCaptureKeyAsync(
                    async () =>
                    {
                        AdjustTriggerLevelAsync(-1);
                        var worker = _triggerAdjustWorker;
                        if (worker != null) await worker.ConfigureAwait(true);
                    },
                    KeyActionType.AdjustTrigger,
                    coalesceIfBusy: () => AdjustTriggerLevelAsync(-1)
                );
                return;
            }

            // PLUS => zoom-in (smaller TIME/DIV)
            if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = ExecuteCaptureKeyAsync(async () => { await AdjustTimeDivAsync(+1); });
                return;
            }

            // MINUS => zoom-out (larger TIME/DIV)
            if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = ExecuteCaptureKeyAsync(async () => { await AdjustTimeDivAsync(-1); });
                return;
            }

            // Numpad Decimal => ClearStatistics
            if (e.KeyCode == Keys.Decimal)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = ExecuteCaptureKeyAsync(async () =>
                {
                    await RunSuiteHeadlessAsync(ScopeTestSuite.ClearStatistics);
                });
                return;
            }
        }

        // Query current TIME/DIV via suite and parse seconds/div as double
        private async Task<double> QueryTimeDivViaSuiteAsync()
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.QueryTimeDiv) + ":");

            var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.QueryTimeDiv);
            string primary = GetPrimaryScpiForSuite(ScopeTestSuite.QueryTimeDiv);

            string timeDivResp = null;

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                bool first = (i == 0);

                if (first)
                {
                    var cmdToSend = (primary ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(cmdToSend))
                    {
                        LogSkipEmpty(step);
                    }
                    else if (IsQuery(step))
                    {
                        var r = await _scope.SendRawQueryAsync(cmdToSend, ct).ConfigureAwait(true);
                        if (step == ScopeCommand.QueryTimeDiv) timeDivResp = r;
                    }
                    else
                    {
                        await _scope.SendRawWriteAsync(cmdToSend, ct).ConfigureAwait(true);
                    }
                }
                else
                {
                    var scpi = GetDefaultScpiForCurrentProfile(step);
                    if (string.IsNullOrWhiteSpace(scpi))
                    {
                        LogSkipEmpty(step);
                        continue;
                    }

                    if (IsQuery(step))
                    {
                        var r = await _scope.SendRawQueryAsync(scpi, ct).ConfigureAwait(true);
                        if (step == ScopeCommand.QueryTimeDiv) timeDivResp = r;
                    }
                    else
                    {
                        await _scope.SendRawWriteAsync(scpi, ct).ConfigureAwait(true);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(timeDivResp))
                throw new InvalidOperationException("No TIME/DIV response received.");

            double seconds;
            if (!double.TryParse(timeDivResp.Trim(),
                                 NumberStyles.Float | NumberStyles.AllowLeadingSign,
                                 CultureInfo.InvariantCulture,
                                 out seconds))
            {
                var m = Regex.Match(timeDivResp ?? string.Empty, @"[+-]?(?:\d+\.?\d*|\d*\.?\d+)(?:[eE][+-]?\d+)?");
                if (!m.Success || !double.TryParse(m.Value,
                                                   NumberStyles.Float | NumberStyles.AllowLeadingSign,
                                                   CultureInfo.InvariantCulture,
                                                   out seconds))
                    throw new InvalidOperationException("Unable to parse TIME/DIV: " + (timeDivResp ?? "(empty)"));
            }

            Logger.Instance.Info("TIME/DIV read as " + FormatSecondsToSi(seconds));
            return seconds;
        }

        // Execute SetTimeDiv suite with a specific target seconds/div
        private async Task RunSetTimeDivSuiteAsync(double targetSeconds)
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.SetTimeDiv) + ":");

            var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.SetTimeDiv);

            string overrideFmt = (txtCmdTimeDivSet?.Text ?? string.Empty).Trim();
            string profileFmt = GetDefaultScpiForCurrentProfile(ScopeCommand.SetTimeDiv) ?? string.Empty;

            string fmtToUse = (!string.IsNullOrWhiteSpace(overrideFmt) && overrideFmt.IndexOf("{0}", StringComparison.Ordinal) >= 0)
                                ? overrideFmt
                                : profileFmt;

            string formattedSeconds = targetSeconds.ToString("0.#########e+0", CultureInfo.InvariantCulture);

            string primaryScpi;
            if (!string.IsNullOrWhiteSpace(fmtToUse) && fmtToUse.IndexOf("{0}", StringComparison.Ordinal) >= 0)
            {
                primaryScpi = string.Format(CultureInfo.InvariantCulture, fmtToUse, formattedSeconds);
            }
            else if (!string.IsNullOrWhiteSpace(fmtToUse))
            {
                primaryScpi = (fmtToUse + " " + formattedSeconds).Trim();
            }
            else
            {
                primaryScpi = formattedSeconds;
            }

            bool first = true;

            foreach (var step in steps)
            {
                if (step == ScopeCommand.DrainSystemErrorQueue)
                {
                    await DrainSystemErrorQueueAsync().ConfigureAwait(true);
                    continue;
                }

                if (first)
                {
                    await _scope.SendRawWriteAsync(primaryScpi, ct).ConfigureAwait(true);
                    first = false;
                    continue;
                }

                if (step == ScopeCommand.DumpImage)
                    continue;

                var scpi = GetDefaultScpiForCurrentProfile(step);
                if (string.IsNullOrWhiteSpace(scpi))
                {
                    LogSkipEmpty(step);
                    continue;
                }

                if (IsQuery(step))
                {
                    await _scope.SendRawQueryAsync(scpi, ct).ConfigureAwait(true);
                }
                else
                {
                    await _scope.SendRawWriteAsync(scpi, ct).ConfigureAwait(true);
                }
            }

            Logger.Instance.Info("TIME/DIV set to " + FormatSecondsToSi(targetSeconds));
        }

        // direction: +1 => zoom-in (smaller time/div), -1 => zoom-out (larger time/div)
        private async Task AdjustTimeDivAsync(int direction)
        {
            // Drop repeat while busy (prevents queue growth on key repeat)
            if (Interlocked.Exchange(ref _timeDivAdjustBusy, 1) == 1)
                return;

            try
            {
                await EnsureConnectedAsync();

                // Capture vendor/model early on UI thread
                var vendor = cboVendor.SelectedItem as string ?? string.Empty;
                var modelDisplay = cboModel.SelectedItem as string ?? string.Empty;
                var model = ToModelPattern(modelDisplay);

                // 1) Query current time/div
                var current = await QueryTimeDivViaSuiteAsync().ConfigureAwait(true);

                // 2) Find next/prev from profile
                double target;
                bool ok = direction >= 0
                    ? ScpiProfileRegistry.TryGetPrevTimeDiv(vendor, model, current, out target) // + => smaller
                    : ScpiProfileRegistry.TryGetNextTimeDiv(vendor, model, current, out target); // - => larger

                if (!ok)
                {
                    Logger.Instance.Info("TIME/DIV is already at the " + (direction >= 0 ? "minimum" : "maximum") + " supported value.");
                    return;
                }

                // 3) Set via the suite
                await RunSetTimeDivSuiteAsync(target).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Adjust TIME/DIV failed: " + InnermostMessage(ex));
            }
            finally
            {
                Interlocked.Exchange(ref _timeDivAdjustBusy, 0);
            }
        }

        private static double ParseVoltsStep(string text)
        {
            // Accept inputs like "0.5V", "1V", "0.25", "1"
            if (string.IsNullOrWhiteSpace(text)) return 0.5;
            var s = text.Trim();
            if (s.EndsWith("V", StringComparison.OrdinalIgnoreCase)) s = s.Substring(0, s.Length - 1);
            double v;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v > 0 ? v : 0.5;
        }

        private double GetSelectedGridStepVolts()
        {
            try
            {
                ComboBox cb = null;
                try { cb = comboBoxAdjustToGrid; } catch { }
                if (cb == null)
                    cb = this.Controls.Find("comboBoxAdjustToGrid", true).FirstOrDefault() as ComboBox;

                var txt = cb?.SelectedItem as string ?? cb?.Text;
                if (string.IsNullOrWhiteSpace(txt))
                    txt = _config?.AdjustToGridStep ?? "0.5V";

                return ParseVoltsStep(txt);
            }
            catch { return 0.5; }
        }

        private static double SnapNext(double current, double step)
        {
            if (step <= 0) return current;

            decimal v = Math.Round((decimal)current, 6, MidpointRounding.AwayFromZero);
            decimal s = (decimal)step;
            decimal q = v / s;
            decimal nearest = Math.Round(q, 0, MidpointRounding.AwayFromZero);

            const decimal tolQ = 0.0002m;

            // If effectively on a grid point, go to the next one; else go up to the next grid point
            decimal k = (Math.Abs(q - nearest) <= tolQ) ? (nearest + 1) : Math.Ceiling(q);
            decimal next = k * s;
            return (double)next;
        }

        private static double SnapPrev(double current, double step)
        {
            if (step <= 0) return current;

            decimal v = Math.Round((decimal)current, 6, MidpointRounding.AwayFromZero);
            decimal s = (decimal)step;
            decimal q = v / s;
            decimal nearest = Math.Round(q, 0, MidpointRounding.AwayFromZero);

            const decimal tolQ = 0.0002m;

            // If effectively on a grid point, go to the previous one; else go down to the previous grid point
            decimal k = (Math.Abs(q - nearest) <= tolQ) ? (nearest - 1) : Math.Floor(q);
            decimal prev = k * s;
            return (double)prev;
        }

        // Replace the body of AdjustTriggerLevelAsync to chain the two suites with snap-to-grid
        private Task AdjustTriggerLevelAsync(int direction)
        {
            // Accumulate net steps; e.g., UP, UP, DOWN => pending = +1
            Interlocked.Add(ref _triggerAdjustPending, direction);

            // Start the pump if not already running
            if (_triggerAdjustWorker == null || _triggerAdjustWorker.IsCompleted)
            {
                _triggerAdjustWorker = TriggerAdjustPumpAsync();
            }
            return Task.CompletedTask;
        }

        // Run an async action on the UI thread
        private Task RunOnUiThreadAsync(Func<Task> action)
        {
            if (this.InvokeRequired)
            {
                var tcs = new TaskCompletionSource<object>();
                try
                {
                    this.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            await action().ConfigureAwait(true);
                            tcs.SetResult(null);
                        }
                        catch (Exception ex) { tcs.SetException(ex); }
                    }));
                }
                catch (Exception ex) { tcs.SetException(ex); }
                return tcs.Task;
            }
            // Already on UI thread
            return action();
        }

        // Run an async function on the UI thread and return its result
        private Task<T> RunOnUiThreadAsync<T>(Func<Task<T>> action)
        {
            if (this.InvokeRequired)
            {
                var tcs = new TaskCompletionSource<T>();
                try
                {
                    this.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            var result = await action().ConfigureAwait(true);
                            tcs.SetResult(result);
                        }
                        catch (Exception ex) { tcs.SetException(ex); }
                    }));
                }
                catch (Exception ex) { tcs.SetException(ex); }
                return tcs.Task;
            }
            // Already on UI thread
            return action();
        }

        // Runs on background, processes accumulated steps in bursts.
        private async Task TriggerAdjustPumpAsync()
        {
            // Ensure only one pump executes at a time
            if (!await _triggerAdjustGate.WaitAsync(0).ConfigureAwait(false))
                return;

            try
            {
                var ct = _cts?.Token ?? CancellationToken.None;

                while (true)
                {
                    // Debounce window: collect rapid key presses
                    await Task.Delay(TriggerAdjustDebounceMs, ct).ConfigureAwait(false);

                    // Pull the current net requested steps (and reset)
                    int delta = Interlocked.Exchange(ref _triggerAdjustPending, 0);
                    if (delta == 0)
                        break; // nothing to do -> exit pump

                    // Guard connectivity (UI thread: touches controls)
                    await RunOnUiThreadAsync(() => EnsureConnectedAsync()).ConfigureAwait(false);

                    // Read step safely (UI access marshaled)
                    double gridStep = SafeGetSelectedGridStepVolts();
                    if (gridStep <= 0) gridStep = 0.5;

                    // 1) Query current trigger via the suite (UI thread: uses UI SCPI overrides)
                    double current = await RunOnUiThreadAsync(() => QueryTriggerLevelViaSuiteAsync()).ConfigureAwait(false);

                    // 2) Compute final target by applying net steps in one go (pure CPU, stays on BG thread)
                    double target = current;
                    int n = Math.Abs(delta);
                    if (delta > 0)
                    {
                        for (int i = 0; i < n; i++) target = SnapNext(target, gridStep);
                    }
                    else
                    {
                        for (int i = 0; i < n; i++) target = SnapPrev(target, gridStep);
                    }
                    target = Math.Round(target, 6, MidpointRounding.AwayFromZero);

                    // 3) Set via the suite (UI thread: uses UI SCPI overrides, runs OPC/SYSERR steps)
                    await RunOnUiThreadAsync(() => RunSetTriggerLevelSuiteAsync(target)).ConfigureAwait(false);

                    // Minimal guard between repeated writes
                    await Task.Delay(TriggerAdjustMinIntervalMs, ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Trigger adjust pump failed: " + InnermostMessage(ex));
            }
            finally
            {
                _triggerAdjustGate.Release();
            }
        }

        // UI-safe reader for the grid step combo/config
        private double SafeGetSelectedGridStepVolts()
        {
            if (InvokeRequired)
            {
                double v = 0.5;
                try { Invoke(new Action(() => v = GetSelectedGridStepVolts())); } catch { }
                return v;
            }
            return GetSelectedGridStepVolts();
        }

        private void WireAdjustToGridComboPersistence()
        {
            var cbAdjust = this.Controls.Find("comboBoxAdjustToGrid", true).FirstOrDefault() as ComboBox;
            if (cbAdjust == null) return;

            // Initialize from config (or persist designer default on first run)
            var saved = _config?.AdjustToGridStep;
            if (!string.IsNullOrWhiteSpace(saved))
            {
                int idx = cbAdjust.Items.IndexOf(saved);
                if (idx >= 0) cbAdjust.SelectedIndex = idx;
                else cbAdjust.Text = saved;
            }
            else
            {
                var current = cbAdjust.Text?.Trim();
                if (!string.IsNullOrEmpty(current))
                {
                    _config.AdjustToGridStep = current;
                    ConfigurationService.Save(_config);
                }
            }

            EventHandler save = (s, a) =>
            {
                var val = cbAdjust.Text?.Trim();
                if ((_config.AdjustToGridStep ?? string.Empty) != (val ?? string.Empty))
                {
                    _config.AdjustToGridStep = val;
                    ConfigurationService.Save(_config);
                }
            };
            cbAdjust.SelectedIndexChanged += save;
            cbAdjust.TextChanged += save;
        }

        // Runs the QueryTriggerLevel suite and returns the current trigger voltage in volts
        private async Task<double> QueryTriggerLevelViaSuiteAsync()
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.QueryTriggerLevel) + ":");

            var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.QueryTriggerLevel);
            string primary = GetPrimaryScpiForSuite(ScopeTestSuite.QueryTriggerLevel);

            string trigResp = null;

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                bool first = (i == 0);

                if (first)
                {
                    var cmdToSend = (primary ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(cmdToSend))
                    {
                        LogSkipEmpty(step);
                    }
                    else if (IsQuery(step))
                    {
                        var r = await _scope.SendRawQueryAsync(cmdToSend, ct).ConfigureAwait(true);
                        if (step == ScopeCommand.QueryTriggerLevel) trigResp = r;
                    }
                    else
                    {
                        await _scope.SendRawWriteAsync(cmdToSend, ct).ConfigureAwait(true);
                    }
                }
                else
                {
                    var scpi = GetDefaultScpiForCurrentProfile(step);
                    if (string.IsNullOrWhiteSpace(scpi))
                    {
                        LogSkipEmpty(step);
                        continue;
                    }

                    if (IsQuery(step))
                    {
                        var r = await _scope.SendRawQueryAsync(scpi, ct).ConfigureAwait(true);
                        if (step == ScopeCommand.QueryTriggerLevel) trigResp = r;
                    }
                    else
                    {
                        await _scope.SendRawWriteAsync(scpi, ct).ConfigureAwait(true);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(trigResp))
                throw new InvalidOperationException("Unable to parse trigger level: (no response)");

            double volts;
            if (!double.TryParse((trigResp ?? string.Empty).Trim(),
                                 NumberStyles.Float | NumberStyles.AllowLeadingSign,
                                 CultureInfo.InvariantCulture,
                                 out volts))
            {
                var m = Regex.Match(trigResp ?? string.Empty, @"[+-]?(?:\d+\.?\d*|\d*\.?\d+)(?:[eE][+-]?\d+)?");
                if (!m.Success || !double.TryParse(m.Value,
                                                   NumberStyles.Float | NumberStyles.AllowLeadingSign,
                                                   CultureInfo.InvariantCulture,
                                                   out volts))
                    throw new InvalidOperationException("Unable to parse trigger level: " + (trigResp ?? "(empty)"));
            }

            Logger.Instance.Info("Trigger level read as " + FormatVoltsToSi(volts));
            return volts;
        }

        // Executes the SetTriggerLevel suite where the first command is formatted with targetVolts
        private async Task RunSetTriggerLevelSuiteAsync(double targetVolts)
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.SetTriggerLevel) + ":");

            var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.SetTriggerLevel);

            string fmt = (txtCmdTrigLevelSet?.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fmt))
                fmt = GetDefaultScpiForCurrentProfile(ScopeCommand.SetTriggerLevel) ?? string.Empty;

            string primaryScpi = InjectNumericArg(fmt, targetVolts);
            if (string.IsNullOrWhiteSpace(primaryScpi))
            {
                var profFmt = GetDefaultScpiForCurrentProfile(ScopeCommand.SetTriggerLevel) ?? ":TRIGGER:EDGE:LEVEL {0}";
                primaryScpi = InjectNumericArg(profFmt, targetVolts);
            }

            bool first = true;

            foreach (var step in steps)
            {
                if (step == ScopeCommand.DrainSystemErrorQueue)
                {
                    await DrainSystemErrorQueueAsync().ConfigureAwait(true);
                    continue;
                }

                if (first)
                {
                    if (!string.IsNullOrWhiteSpace(primaryScpi))
                        await _scope.SendRawWriteAsync(primaryScpi, ct).ConfigureAwait(true);
                    else
                        await _scope.SetTriggerLevelAsync(targetVolts, ct).ConfigureAwait(true);

                    first = false;
                    continue;
                }

                if (step == ScopeCommand.DumpImage)
                    continue;

                var scpi = GetDefaultScpiForCurrentProfile(step);
                if (string.IsNullOrWhiteSpace(scpi))
                {
                    LogSkipEmpty(step);
                    continue;
                }

                if (IsQuery(step))
                {
                    await _scope.SendRawQueryAsync(scpi, ct).ConfigureAwait(true);
                }
                else
                {
                    await _scope.SendRawWriteAsync(scpi, ct).ConfigureAwait(true);
                }
            }

            Logger.Instance.Info("Trigger level set to " + FormatVoltsToSi(targetVolts));
        }

        private static string FormatSecondsToSi(double seconds)
        {
            double abs = Math.Abs(seconds);
            if (abs >= 1.0)
                return seconds.ToString("0.######", CultureInfo.InvariantCulture) + "S";
            if (abs >= 1e-3)
                return (seconds * 1e3).ToString("0.######", CultureInfo.InvariantCulture) + "mS";
            if (abs >= 1e-6)
                return (seconds * 1e6).ToString("0.######", CultureInfo.InvariantCulture) + "uS";
            return (seconds * 1e9).ToString("0.######", CultureInfo.InvariantCulture) + "nS";
        }

        private string GetPrimaryScpiForSuite(ScopeTestSuite suite)
        {
            switch (suite)
            {
                case ScopeTestSuite.QueryIdentify: return txtCmdIdentify?.Text ?? string.Empty;
                case ScopeTestSuite.ClearStatistics: return txtCmdClearStats?.Text ?? string.Empty;
                case ScopeTestSuite.DrainSystemErrorQueue: return txtCmdSysErr?.Text ?? string.Empty;
                case ScopeTestSuite.OperationComplete: return txtCmdOpc?.Text ?? string.Empty;
                case ScopeTestSuite.QueryActiveTrigger: return txtCmdActiveTrig?.Text ?? string.Empty;
                case ScopeTestSuite.Stop: return txtCmdStop?.Text ?? string.Empty;
                case ScopeTestSuite.Single: return txtCmdSingle?.Text ?? string.Empty;
                case ScopeTestSuite.Run: return txtCmdRun?.Text ?? string.Empty;
                case ScopeTestSuite.QueryTriggerMode: return txtCmdTrigMode?.Text ?? string.Empty;
                case ScopeTestSuite.QueryTriggerLevel: return txtCmdTrigLevelQ?.Text ?? string.Empty;
                case ScopeTestSuite.SetTriggerLevel: return txtCmdTrigLevelSet?.Text ?? string.Empty;
                case ScopeTestSuite.QueryTimeDiv: return txtCmdTimeDivQ?.Text ?? string.Empty;
                case ScopeTestSuite.SetTimeDiv: return txtCmdTimeDivSet?.Text ?? string.Empty;
                case ScopeTestSuite.DumpImage: return txtCmdDumpImage?.Text ?? string.Empty;
                default: return string.Empty;
            }
        }

        // Helper: format volts to human-readable SI (V, mV, uV, nV)
        private static string FormatVoltsToSi(double volts)
        {
            double abs = Math.Abs(volts);
            if (abs >= 1.0)
                return volts.ToString("0.######", CultureInfo.InvariantCulture) + "V";
            if (abs >= 1e-3)
                return (volts * 1e3).ToString("0.######", CultureInfo.InvariantCulture) + "mV";
            if (abs >= 1e-6)
                return (volts * 1e6).ToString("0.######", CultureInfo.InvariantCulture) + "uV";
            return (volts * 1e9).ToString("0.######", CultureInfo.InvariantCulture) + "nV";
        }

        private async Task RunSuiteHeadlessAsync(ScopeTestSuite suite)
        {
            var steps = ScopeTestSuiteRegistry.Resolve(_config, suite);
            string primary = GetPrimaryScpiForSuite(suite);
            string lastQueryResponse = null;

            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(suite) + ":");

            foreach (var step in steps)
            {
                if (step == ScopeCommand.DrainSystemErrorQueue)
                {
                    await DrainSystemErrorQueueAsync().ConfigureAwait(true);
                    lastQueryResponse = "0";
                    continue;
                }

                if (step == steps.First())
                {
                    var cmdToSend = (primary ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(cmdToSend))
                    {
                        LogSkipEmpty(step);
                        continue;
                    }

                    if (IsQuery(step))
                        lastQueryResponse = await _scope.SendRawQueryAsync(cmdToSend, _cts?.Token ?? CancellationToken.None);
                    else if (step != ScopeCommand.DumpImage)
                        await _scope.SendRawWriteAsync(cmdToSend, _cts?.Token ?? CancellationToken.None);
                }
                else
                {
                    if (step == ScopeCommand.DumpImage)
                        continue;

                    var scpi = GetDefaultScpiForCurrentProfile(step);
                    if (string.IsNullOrWhiteSpace(scpi))
                    {
                        LogSkipEmpty(step);
                        continue; // Skip empty profile step
                    }

                    if (IsQuery(step))
                        lastQueryResponse = await _scope.SendRawQueryAsync(scpi, _cts?.Token ?? CancellationToken.None);
                    else
                        await _scope.SendRawWriteAsync(scpi, _cts?.Token ?? CancellationToken.None);
                }
            }

            if (suite == ScopeTestSuite.QueryIdentify && !string.IsNullOrWhiteSpace(lastQueryResponse))
            {
                LogIdnVendorModelFirmware(lastQueryResponse);
            }

            switch (suite)
            {
                case ScopeTestSuite.ClearStatistics:
                    Logger.Instance.Info("Statistics cleared");
                    break;
                case ScopeTestSuite.QueryActiveTrigger:
                    {
                        var s = TrimQuotes(lastQueryResponse ?? string.Empty).Trim();
                        Logger.Instance.Info("Trigger read as " + (string.IsNullOrEmpty(s) ? "(empty)" : s));
                        break;
                    }
                case ScopeTestSuite.Stop:
                    Logger.Instance.Info("Trigger set to STOP");
                    break;
                case ScopeTestSuite.Run:
                    Logger.Instance.Info("Trigger set to RUN");
                    break;
                case ScopeTestSuite.Single:
                    Logger.Instance.Info("Trigger set to SINGLE");
                    break;
                case ScopeTestSuite.QueryTriggerMode:
                    {
                        var s = TrimQuotes(lastQueryResponse ?? string.Empty).Trim();
                        Logger.Instance.Info("Trigger mode read as " + (string.IsNullOrEmpty(s) ? "(empty)" : s));
                        break;
                    }
                case ScopeTestSuite.DumpImage:
                    Logger.Instance.Info("Dumped image from raw network stream.");
                    break;
                case ScopeTestSuite.DrainSystemErrorQueue:
                    Logger.Instance.Info("Popped last system error.");
                    break;
                case ScopeTestSuite.OperationComplete:
                    Logger.Instance.Info("Operation completed.");
                    break;
            }
        }

        private async Task CaptureAndSaveAsync()
        {
            bool savedOk = false;

            try
            {
                // Show IndianRed overlay while capturing
                ShowWaitOverlay("Wait ...");

                if (_config != null && _config.EnableBeep) BeepStart();

                var ct = _cts?.Token ?? CancellationToken.None;
                bool skipClearAndStop = false; // when true, jump directly to "capture" section

                if (_config != null && _config.ForceClear)
                {
                    // If user asked to avoid clearing when we're already in STOP mode
                    if (_config.DoNotClearWhenStop)
                    {
                        try
                        {
                            await EnsureConnectedAsync();

                            // Resolve UI-effective SCPI for QueryActiveTrigger (suite has only this single command)
                            var cmdActive = GetPrimaryScpiForSuite(ScopeTestSuite.QueryActiveTrigger);
                            if (string.IsNullOrWhiteSpace(cmdActive))
                                cmdActive = GetDefaultScpiForCurrentProfile(ScopeCommand.QueryActiveTrigger);

                            if (!string.IsNullOrWhiteSpace(cmdActive))
                            {
                                var resp = await _scope.SendRawQueryAsync(cmdActive, ct).ConfigureAwait(true);
                                var s = TrimQuotes(resp ?? string.Empty).Trim();

                                if (!string.IsNullOrEmpty(s) &&
                                    s.IndexOf("stop", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    // Already in STOP → skip Clear/Delay/Stop and go straight to capture
                                    Logger.Instance.Info("Scope already in STOP; skipping Clear/Delay/Stop.");
                                    skipClearAndStop = true;
                                }
                            }
                            else
                            {
                                Logger.Instance.Debug("QueryActiveTrigger command empty; cannot determine STOP state.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error("QueryActiveTrigger failed: " + InnermostMessage(ex));
                        }
                    }

                    // Only clear + delay + stop if not explicitly skipping
                    if (!skipClearAndStop)
                    {
                        try
                        {
                            await RunSuiteHeadlessAsync(ScopeTestSuite.ClearStatistics);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error("ClearStatistics suite failed before capture: " + InnermostMessage(ex));
                        }

                        try
                        {
                            var delayMs = Math.Max(0, _config?.DelayMs ?? 0);
                            if (delayMs > 0)
                            {
                                Logger.Instance.Debug("---");
                                Logger.Instance.Debug("Delay before capture: " + delayMs.ToString(CultureInfo.InvariantCulture) + " ms");
                                await Task.Delay(delayMs, ct);
                                Logger.Instance.Debug("Delay completed.");
                            }
                        }
                        catch (TaskCanceledException) { }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error("Delay after ClearStatistics failed: " + InnermostMessage(ex));
                        }

                        // Stop acquisition before DumpImage (only when not skipping this phase)
                        try
                        {
                            await RunSuiteHeadlessAsync(ScopeTestSuite.Stop);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error("Stop suite before capture failed: " + InnermostMessage(ex));
                        }
                    }
                }
                else
                {
                    // Original behavior when ForceClear is disabled: Stop before dump
                    try
                    {
                        await RunSuiteHeadlessAsync(ScopeTestSuite.Stop);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error("Stop suite before capture failed: " + InnermostMessage(ex));
                    }
                }

                // Perform capture using UI-effective SCPI (not the core default)
                var data = await CaptureScreenViaUiAsync(ct);
                if (data == null || data.Length == 0)
                {
                    Logger.Instance.Error("Capture returned no data.");
                    return;
                }

                using (var ms = new MemoryStream(data))
                using (var img = Image.FromStream(ms))
                {
                    var folder = ResolveCaptureFolder();
                    Directory.CreateDirectory(folder);

                    var baseName = BuildFileNameFromFormat();
                    if (string.IsNullOrWhiteSpace(baseName)) baseName = "capture";
                    baseName = SanitizeFileName(baseName);

                    var fileName = baseName + ".png";
                    var path = Path.Combine(folder, fileName);

                    try { if (File.Exists(path)) File.Delete(path); } catch { }

                    img.Save(path, System.Drawing.Imaging.ImageFormat.Png);

                    try
                    {
                        var clone = (Image)img.Clone();
                        var old = picScreen.Image;
                        picScreen.Image = clone;
                        try { old?.Dispose(); } catch { }
                    }
                    catch { }

                    try
                    {
                        long bytes = 0;
                        try { bytes = new FileInfo(path).Length; } catch { }
                        var kib = bytes / 1024.0;
                        int bpp = Image.GetPixelFormatSize(img.PixelFormat);
                        Logger.Instance.Info(string.Format(
                            CultureInfo.InvariantCulture,
                            "Saved capture to {0} ({1}x{2}px, {3}bpp PNG, {4:F1} KB)",
                            path, img.Width, img.Height, bpp, kib));
                    }
                    catch
                    {
                        Logger.Instance.Info("Saved capture to " + path);
                    }

                    _savedFileHistory.Add(fileName);
                    savedOk = true;
                }

                // Optional resume acquisition
                if (_config != null && _config.ForceAcquisition)
                {
                    try
                    {
                        await RunSuiteHeadlessAsync(ScopeTestSuite.Run);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error("Run suite after capture failed: " + InnermostMessage(ex));
                    }
                }

                if (savedOk && _captureMode)
                {
                    IncrementNumberAfterSuccessfulSave();
                    UpdateActionRichTextForNextFilename();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Capture failed: " + InnermostMessage(ex));
            }
            finally
            {
                // Always hide overlay at the end
                HideWaitOverlay();

                if (_config != null && _config.EnableBeep) BeepEnd();
            }
        }

        private void InitializeTriggerSetButtonState()
        {
            try
            {
                if (btnTestTrigLevelSet != null)
                {
                    btnTestTrigLevelSet.Enabled = false;
                    btnTestTrigLevelSet.Tag = null; // will hold a double after successful query
                }
                // Also initialize TIME/DIV Set button
                if (btnTestTimeDivSet != null)
                {
                    btnTestTimeDivSet.Enabled = false;
                    btnTestTimeDivSet.Tag = null; // will hold a double (seconds/div) after successful query
                }
            }
            catch { }
        }

        private void BeepStart()
        {
            try { System.Threading.Tasks.Task.Run(() => { try { Console.Beep(1200, 120); } catch { } }); } catch { }
        }
        private void BeepEnd()
        {
            try { System.Threading.Tasks.Task.Run(() => { try { Console.Beep(800, 180); } catch { } }); } catch { }
        }

        private void BeepDelete()
        {
            try
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        // Two short down tones to distinguish from capture beeps
                        Console.Beep(600, 150);
                        Console.Beep(600, 150);
                    }
                    catch { }
                });
            }
            catch { }
        }

        private string ResolveCaptureFolder()
        {
            var path = _tbCaptureFolder?.Text;
            if (string.IsNullOrWhiteSpace(path)) path = _config?.CaptureFolder;
            if (string.IsNullOrWhiteSpace(path)) path = Application.StartupPath;
            return path;
        }

        private string BuildFileNameFromFormat()
        {
            var fmt = _tbFilenameFormat?.Text ?? _config?.FilenameFormat ?? string.Empty;
            // Build variable dictionary
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Internal date/time variables
            var now = DateTime.Now;
            map["DATE"] = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture); // e.g., 20251231
            map["TIME"] = now.ToString("HHmmss", CultureInfo.InvariantCulture);   // e.g., 235959

            map["NUMBER"] = (numericUpDown1?.Value ?? 0).ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < (_config?.VariableCount ?? 0); i++)
            {
                var rawName = _config.VariableNames.ElementAtOrDefault(i) ?? string.Empty;
                // Fallback to the implicit default token VAR{i+1} if name not explicitly set
                var name = string.IsNullOrWhiteSpace(rawName) ? ("VAR" + (i + 1)) : rawName;
                var val = _config.VariableValues.ElementAtOrDefault(i) ?? string.Empty;
                map[name.Trim().ToUpperInvariant()] = val ?? string.Empty;
            }

            // Replace {VAR} tokens
            var result = Regex.Replace(fmt ?? string.Empty, "\\{([^}]+)\\}", m =>
            {
                var key = (m.Groups[1].Value ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(key)) return string.Empty;
                if (map.TryGetValue(key.ToUpperInvariant(), out var v)) return v ?? string.Empty;
                return string.Empty;
            });

            // Post-process underscores
            if (_config != null)
            {
                if (_config.DeleteDoubleUnderscore)
                {
                    result = Regex.Replace(result, "[ _]{2,}", "_");
                }
                if (_config.TrimUnderscore)
                {
                    result = result.Trim(' ', '_');
                }
            }
            return result;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var ch in name)
            {
                sb.Append(invalid.Contains(ch) ? '_' : ch);
            }
            var s = sb.ToString();
            if (string.IsNullOrWhiteSpace(s)) s = "capture";
            return s;
        }

        private static async Task WithButtonDisabledAsync(Button btn, Label statusLabel, Func<Task> action)
        {
            if (btn == null || action == null) return;
            try
            {
                btn.Enabled = false;
                if (statusLabel != null)
                {
                    statusLabel.Text = "Testing...";
                    statusLabel.ForeColor = Color.Gray;
                }
                await action().ConfigureAwait(true);
            }
            finally
            {
                btn.Enabled = true;
            }
        }

        // Mark connectable only after a successful connection
        private async Task EnsureConnectedAsync()
        {
            if (_scope != null && _scope.IsConnected) return;

            var vendor = cboVendor.SelectedItem as string;
            var modelDisplay = cboModel.SelectedItem as string;
            var modelPattern = ToModelPattern(modelDisplay);
            var resource = ComposeResource();

            if (string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(modelPattern))
                throw new InvalidOperationException("Select vendor and model.");

            try { if (_scope != null) await _scope.DisconnectAsync(); } catch { }

            _scope = ScopeFactory.Create(vendor, modelPattern, resource);

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            Logger.Instance.Info(string.Format("Auto-connecting to {0} {1} at {2}:", vendor, modelDisplay, resource));
            await _scope.ConnectAsync(_cts.Token);
            _isConnectable = true;                 // <-- set flag on success
            Logger.Instance.Debug("Auto-connected.");
        }

        private static bool TryParseSystemErrorCode(string resp, out int code)
        {
            code = 0;
            if (string.IsNullOrWhiteSpace(resp)) return false;
            var s = resp.Trim();

            var m = Regex.Match(s, @"[+-]?\d+");
            if (m.Success)
            {
                if (int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out code))
                {
                    return true;
                }
            }
            if (s.IndexOf("no error", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                code = 0;
                return true;
            }
            return false;
        }

        private async Task<byte[]> CaptureScreenViaUiAsync(CancellationToken ct)
        {
            // Mirror the Test button behavior, but as a reusable helper for capture mode
            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.DumpImage) + ":");

            var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.DumpImage);

            // Execute pre-steps (all steps except DumpImage) using effective (UI-first) SCPI
            foreach (var step in steps)
            {
                if (step == ScopeCommand.DumpImage) continue;

                var scpi = GetDefaultScpiForCurrentProfile(step);
                if (string.IsNullOrWhiteSpace(scpi))
                {
                    LogSkipEmpty(step);
                    continue;
                }

                if (IsQuery(step))
                    await _scope.SendRawQueryAsync(scpi, ct).ConfigureAwait(true);
                else
                    await _scope.SendRawWriteAsync(scpi, ct).ConfigureAwait(true);
            }

            // DumpImage from UI; empty => skip/abort
            var cmd = ResolveUiScpiFor(ScopeCommand.DumpImage);
            if (string.IsNullOrWhiteSpace(cmd))
                throw new InvalidOperationException("DumpImage command is empty.");

            // Read raw IEEE 488.2 block and strip header for image decode
            var raw = await _scope.SendRawDumpAndReadAsync(cmd, ct).ConfigureAwait(true);
            return StripIeee4882Block(raw);
        }

        private async Task DrainSystemErrorQueueAsync()
        {
            await EnsureConnectedAsync();
            var ct = _cts?.Token ?? CancellationToken.None;

            // Prefer UI textbox; GetDefaultScpiForCurrentProfile already resolves UI-first
            var scpi = GetDefaultScpiForCurrentProfile(ScopeCommand.DrainSystemErrorQueue);
            if (string.IsNullOrWhiteSpace(scpi))
            {
                // Intentionally skipped for this vendor/model
                Logger.Instance.Debug("Skipping system error drain (command empty).");
                return;
            }

            int drained = 0;
            while (true)
            {
                string resp;
                try
                {
                    resp = await _scope.SendRawQueryAsync(scpi, ct).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("System error query failed: " + InnermostMessage(ex));
                    break;
                }

                // Log each read
                Logger.Instance.Debug("System error: " + (resp ?? string.Empty));

                int code;
                if (!TryParseSystemErrorCode(resp, out code))
                    break; // unparsed -> stop

                if (code == 0)
                    break; // "no error" -> drained

                drained++;
            }

            if (drained > 0)
            {
                Logger.Instance.Info("Drained " + drained.ToString(CultureInfo.InvariantCulture) + " system error(s).");
            }
        }

        private static string MaskIdnSerialToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return token;
            var inner = TrimQuotes(token).Trim();
            if (inner.Length == 0) return token;
            var masked = new string('*', inner.Length);
            if ((token.StartsWith("\"") && token.EndsWith("\"")) || (token.StartsWith("'") && token.EndsWith("'")))
                return token.Substring(0, 1) + masked + token.Substring(token.Length - 1, 1);
            return masked;
        }

        private string MaskSerialInIdnDebugLine(string line)
        {
            try
            {
                if (string.IsNullOrEmpty(line)) return line;
                const string marker = "SCPI <<";
                var idx = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return line;

                var after = idx + marker.Length;
                while (after < line.Length && char.IsWhiteSpace(line[after])) after++;

                var payload = line.Substring(after);
                var parts = payload.Split(new[] { ',' }, StringSplitOptions.None);
                if (parts.Length < 4) return line;

                // Heuristic: mask 3rd CSV token (serial) if it looks like a serial
                var inner = TrimQuotes(parts[2]).Trim();
                if (inner.Length < 6) return line; // avoid masking unrelated short tokens

                parts[2] = MaskIdnSerialToken(parts[2]);
                var rebuilt = string.Join(",", parts);
                return line.Substring(0, after) + rebuilt;
            }
            catch
            {
                return line; // best-effort
            }
        }

        private static string TrimQuotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
                return s.Substring(1, s.Length - 2);
            return s;
        }

        private static (string Vendor, string Model, string Serial, string Firmware) ParseIdn(string idn)
        {
            if (string.IsNullOrWhiteSpace(idn)) return ("", "", "", "");
            var parts = idn
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => TrimQuotes(p).Trim())
                .ToArray();

            string vendor = parts.Length > 0 ? parts[0] : "";
            string model = parts.Length > 1 ? parts[1] : "";

            // Standard SCPI *IDN? format: VENDOR,MODEL,SERIAL,FIRMWARE
            // If only 3 fields are present, treat the 3rd as Firmware and leave Serial empty.
            string serial = parts.Length >= 4 ? parts[2] : "";
            string firmware = parts.Length >= 4 ? parts[3] : (parts.Length == 3 ? parts[2] : "");

            return (vendor, model, serial, firmware);
        }

        private void RestoreWindowLayout()
        {
            try
            {
                if (_config == null) return;
                if (_config.WindowWidth > 0 && _config.WindowHeight > 0)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Size = new Size(_config.WindowWidth, _config.WindowHeight);
                }
                if (_config.WindowMaximized)
                {
                    this.WindowState = FormWindowState.Maximized;
                }
            }
            catch { }
        }

        private void PersistWindowLayout()
        {
            try
            {
                if (_suppressLayoutSave) return;
                if (_config == null) return;
                if (this.WindowState == FormWindowState.Minimized) return;
                _config.WindowMaximized = (this.WindowState == FormWindowState.Maximized);
                var b = this.WindowState == FormWindowState.Normal ? this.Bounds : this.RestoreBounds;
                _config.WindowWidth = Math.Max(100, b.Width);
                _config.WindowHeight = Math.Max(100, b.Height);
                ConfigurationService.Save(_config);
            }
            catch { }
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            PersistWindowLayout();

            // Center the delete overlay label if visible
            if (_deleteOverlayLabel != null && _deleteOverlayLabel.Visible && picScreen != null)
            {
                var x = Math.Max(0, (picScreen.ClientSize.Width - _deleteOverlayLabel.Width) / 2);
                var y = Math.Max(0, (picScreen.ClientSize.Height - _deleteOverlayLabel.Height) / 2);
                _deleteOverlayLabel.Location = new Point(x, y);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PersistWindowLayout();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            PersistWindowLayout();
            try { StopConnectionMonitor(); } catch { }
            base.OnFormClosed(e);
        }

        private void EnsureVariableNamesListSize()
        {
            if (_config.VariableNames == null) _config.VariableNames = new List<string>();
            if (_config.VariableValues == null) _config.VariableValues = new List<string>();
            while (_config.VariableNames.Count < _config.VariableCount) _config.VariableNames.Add(string.Empty);
            while (_config.VariableNames.Count > _config.VariableCount) _config.VariableNames.RemoveAt(_config.VariableNames.Count - 1);
            while (_config.VariableValues.Count < _config.VariableCount) _config.VariableValues.Add(string.Empty);
            while (_config.VariableValues.Count > _config.VariableCount) _config.VariableValues.RemoveAt(_config.VariableValues.Count - 1);
        }

        private void BuildVariableNameEditors()
        {
            if (tabSettings == null) return;
            EnsureVariableNamesListSize();

            // Remove existing dynamic controls
            var toRemove = tabSettings.Controls.Cast<Control>()
                .Where(c => (c.Name ?? string.Empty).StartsWith(VarNameEditorPrefix) || (c.Name ?? string.Empty).StartsWith(VarNameLabelPrefix))
                .ToList();
            foreach (var c in toRemove) { try { tabSettings.Controls.Remove(c); c.Dispose(); } catch { } }

            // Position just below numericUpDownVariables with tighter spacing
            int baseY = (numericUpDownVariables != null) ? numericUpDownVariables.Bottom + 6 : 320;
            int xText = 13;
            int rowH = 26;

            int nameTbWidth = numericUpDownVariables != null ? numericUpDownVariables.Width : 220;
            int xLabel = xText + nameTbWidth + 6; // label closer to textbox

            // Start TabIndex right after numericUpDownVariables
            int startTab = ((numericUpDownVariables != null) ? numericUpDownVariables.TabIndex : 0) + 1;

            // Dynamic variables (editable)
            for (int i = 0; i < (_config?.VariableCount ?? 0); i++)
            {
                // Textbox first: variable name editor (uppercase)
                var tb = new TextBox
                {
                    Name = VarNameEditorPrefix + i,
                    Location = new Point(xText, baseY + i * rowH),
                    Width = nameTbWidth,
                    Text = (_config.VariableNames.ElementAtOrDefault(i) ?? ("VAR" + (i + 1))),
                    TabIndex = startTab + i
                };
                try { tb.CharacterCasing = CharacterCasing.Upper; } catch { }
                int idx = i;
                tb.TextChanged += (s, a) =>
                {
                    EnsureVariableNamesListSize();
                    var upper = (tb.Text ?? string.Empty).Trim().ToUpperInvariant();
                    _config.VariableNames[idx] = upper;
                    ConfigurationService.Save(_config);
                    UpdateVariablesUI(_config.VariableCount);
                    // Update the label next to this editor
                    var nameLblRef = tabSettings.Controls.Find(VarNameLabelPrefix + idx, true).FirstOrDefault() as Label;
                    if (nameLblRef != null)
                    {
                        var nm = string.IsNullOrWhiteSpace(upper) ? ("VAR" + (idx + 1)) : upper;
                        nameLblRef.Text = "{" + nm + "}";
                        try { nameLblRef.Font = label2?.Font ?? nameLblRef.Font; } catch { }
                    }
                    UpdateActionRichTextForNextFilename();
                };
                tabSettings.Controls.Add(tb);

                // Label to the right: shows current variable name token {NAME}; skip in tab order
                var nameLbl = new Label
                {
                    Name = VarNameLabelPrefix + i,
                    AutoSize = true,
                    Location = new Point(xLabel, baseY + i * rowH + 3),
                    TabStop = false
                };
                var nmTxt = _config.VariableNames.ElementAtOrDefault(i);
                if (string.IsNullOrWhiteSpace(nmTxt)) nmTxt = "VAR" + (i + 1);
                nameLbl.Text = "{" + nmTxt.ToUpperInvariant() + "}";
                try { nameLbl.Font = label2?.Font ?? nameLbl.Font; } catch { }
                tabSettings.Controls.Add(nameLbl);
            }

            // Append permanent, read-only system variables as the last two rows
            int afterDynamicY = baseY + (_config?.VariableCount ?? 0) * rowH;

            // DATE row
            var tbDate = new TextBox
            {
                Name = VarNameEditorPrefix + "DATE",
                Location = new Point(xText, afterDynamicY),
                Width = nameTbWidth,
                Text = "DATE",
                ReadOnly = true,
                TabStop = false
            };
            try { tbDate.CharacterCasing = CharacterCasing.Upper; } catch { }
            tabSettings.Controls.Add(tbDate);

            var lblDate = new Label
            {
                Name = VarNameLabelPrefix + "DATE",
                AutoSize = true,
                Location = new Point(xLabel, afterDynamicY + 3),
                Text = "{DATE}",
                TabStop = false
            };
            try { lblDate.Font = label2?.Font ?? lblDate.Font; } catch { }
            tabSettings.Controls.Add(lblDate);

            // TIME row
            var tbTime = new TextBox
            {
                Name = VarNameEditorPrefix + "TIME",
                Location = new Point(xText, afterDynamicY + rowH),
                Width = nameTbWidth,
                Text = "TIME",
                ReadOnly = true,
                TabStop = false
            };
            try { tbTime.CharacterCasing = CharacterCasing.Upper; } catch { }
            tabSettings.Controls.Add(tbTime);

            var lblTime = new Label
            {
                Name = VarNameLabelPrefix + "TIME",
                AutoSize = true,
                Location = new Point(xLabel, afterDynamicY + rowH + 3),
                Text = "{TIME}",
                TabStop = false
            };
            try { lblTime.Font = label2?.Font ?? lblTime.Font; } catch { }
            tabSettings.Controls.Add(lblTime);

            // Relocate file name cleanup checkboxes under the last (fixed) row
            int endY = afterDynamicY + (2 * rowH) + 8;
            try
            {
                if (checkBoxDeleteDoubleUnderscore != null)
                {
                    checkBoxDeleteDoubleUnderscore.Location = new Point(checkBoxDeleteDoubleUnderscore.Location.X, endY);
                    endY = checkBoxDeleteDoubleUnderscore.Bottom + 4;
                }
                if (checkBoxTrimUnderscore != null)
                {
                    checkBoxTrimUnderscore.Location = new Point(checkBoxTrimUnderscore.Location.X, endY);
                }
            }
            catch { }
        }

        private void UpdateVariablesUI(int desired)
        {
            if (tabCapturing == null) return;
            EnsureVariableNamesListSize();

            // Remove existing variable controls
            var toRemove = tabCapturing.Controls.Cast<Control>()
                .Where(c => (c.Name ?? string.Empty).StartsWith(VarLabelPrefix) || (c.Name ?? string.Empty).StartsWith(VarTextPrefix))
                .ToList();
            foreach (var c in toRemove) { try { tabCapturing.Controls.Remove(c); c.Dispose(); } catch { } }

            // Place variables directly under numericUpDown1
            int baseY = (numericUpDown1 != null) ? numericUpDown1.Bottom + 6 : (label10 != null ? label10.Bottom + 8 : 260);

            // Align left and width to numericUpDown1
            int xText = (numericUpDown1 != null) ? numericUpDown1.Left : 8;
            int textWidth = (numericUpDown1 != null) ? numericUpDown1.Width : 180;

            // Keep token labels aligned with the {NUMBER} label if present
            int xLabel = (label2 != null) ? label2.Left : (xText + textWidth + 10);
            int rowH = 26;

            // NEW: start TabIndex right after numericUpDown1 (which is 4 -> start at 5)
            int startTab = ((numericUpDown1 != null) ? numericUpDown1.TabIndex : 4) + 1;

            EventHandler clearAltStatus = (s, a) => ClearLabel15IfCapturing();

            numericUpDown1.Enter += clearAltStatus;
            numericUpDown1.Click += clearAltStatus;
            textBoxFilenameFormat.Enter += clearAltStatus;
            textBoxFilenameFormat.Click += clearAltStatus;


            // Dynamic (editable) variables
            for (int i = 0; i < desired; i++)
            {
                // Textbox first: variable value editor
                var tb = new TextBox
                {
                    Name = VarTextPrefix + i,
                    Location = new Point(xText, baseY + i * rowH),
                    Width = textWidth,
                    Text = _config.VariableValues.ElementAtOrDefault(i) ?? string.Empty,
                    TabIndex = startTab + i // <-- ensure tab order 5, 6, ...
                };

                tb.Enter += clearAltStatus;
                tb.Click += clearAltStatus;

                int idx = i;
                tb.TextChanged += (s, a) =>
                {
                    EnsureVariableNamesListSize();
                    _config.VariableValues[idx] = tb.Text ?? string.Empty;
                    ConfigurationService.Save(_config);
                    UpdateActionRichTextForNextFilename();
                };
                tabCapturing.Controls.Add(tb);

                // Label: token name {NAME} (not focusable)
                var name = _config.VariableNames.ElementAtOrDefault(i);
                if (string.IsNullOrWhiteSpace(name)) name = "VAR" + (i + 1);
                var lbl = new Label
                {
                    Name = VarLabelPrefix + i,
                    AutoSize = true,
                    Location = new Point(xLabel, baseY + i * rowH + 3),
                    Text = "{" + name.ToUpperInvariant() + "}",
                    TabStop = false
                };
                try { lbl.Font = label2?.Font ?? lbl.Font; } catch { }
                tabCapturing.Controls.Add(lbl);
            }

            // Append permanent, read-only system variables as the last two rows
            int afterDynamicY = baseY + desired * rowH;
            var now = DateTime.Now;

            // DATE row
            var tbDateVal = new TextBox
            {
                Name = VarTextPrefix + "DATE",
                Location = new Point(xText, afterDynamicY),
                Width = textWidth,
                Text = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                ReadOnly = true,
                TabStop = false
            };
            tabCapturing.Controls.Add(tbDateVal);

            var lblDate = new Label
            {
                Name = VarLabelPrefix + "DATE",
                AutoSize = true,
                Location = new Point(xLabel, afterDynamicY + 3),
                Text = "{DATE}",
                TabStop = false
            };
            try { lblDate.Font = label2?.Font ?? lblDate.Font; } catch { }
            tabCapturing.Controls.Add(lblDate);

            // TIME row
            var tbTimeVal = new TextBox
            {
                Name = VarTextPrefix + "TIME",
                Location = new Point(xText, afterDynamicY + rowH),
                Width = textWidth,
                Text = now.ToString("HHmmss", CultureInfo.InvariantCulture),
                ReadOnly = true,
                TabStop = false
            };
            tabCapturing.Controls.Add(tbTimeVal);

            var lblTime = new Label
            {
                Name = VarLabelPrefix + "TIME",
                AutoSize = true,
                Location = new Point(xLabel, afterDynamicY + rowH + 3),
                Text = "{TIME}",
                TabStop = false
            };
            try { lblTime.Font = label2?.Font ?? lblTime.Font; } catch { }
            tabCapturing.Controls.Add(lblTime);

            // Relocate capture start button under dynamic + 2 fixed rows
            try
            {
                if (buttonCaptureStart != null)
                {
                    int totalRows = desired + 2;
                    int newY = baseY + totalRows * rowH + 10;

                    int btnX = buttonCaptureStart.Location.X;
                    buttonCaptureStart.Location = new Point(btnX, newY);
                    // Optional: move button after variables in tab order:
                    // buttonCaptureStart.TabIndex = startTab + desired;

                    var altStatus = (label15 != null ? label15 : this.Controls.Find("label15", true).FirstOrDefault() as Label);
                    if (altStatus != null)
                    {
                        int lblX = btnX;
                        int lblY = buttonCaptureStart.Bottom + 4;
                        altStatus.Location = new Point(lblX, lblY);
                    }
               }
            }
            catch { }
        }

        private void ClearLabel15IfCapturing()
        {
            try
            {
                // Only clear when Capturing tab is active
                var capturingTab = tabCapturing ?? this.Controls.Find("tabCapturing", true).FirstOrDefault() as TabPage;
                if (tabMain != null && capturingTab != null && tabMain.SelectedTab == capturingTab)
                {
                    if (label15.Text == "Success - you are now connected to the oscilloscope, and capture mode has been enabled")
                    {
                        label15.Text = string.Empty;
                    }
                }
            }
            catch { /* best-effort */ }
        }

        private void buttonDebugClear_Click(object sender, EventArgs e)
        {
            try
            {
                rtbLog.Clear();
                rtbLog.Focus();
                Logger.Instance.ClearLog();
                Logger.Instance.Info("Log cleared by user.");
            }
            catch { }
        }

        // Update flag and UI in the explicit connect flow
        private async Task ConnectAndRefreshAsync()
        {
            Logger.Instance.Debug("---");
            try
            {
                SetConnectivityStatus("Checking...", Color.Gray);

                var vendor = cboVendor.SelectedItem as string;
                var modelDisplay = cboModel.SelectedItem as string;
                var modelPattern = ToModelPattern(modelDisplay);

                _config.ScopeIp = txtIp.Text?.Trim();
                _config.ScopePort = (int)numPort.Value;
                _config.Vendor = vendor;
                _config.Model = modelPattern;
                SaveConnectionSettings();
                ConfigurationService.Save(_config);

                var resource = ComposeResource();

                if (string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(modelPattern))
                {
                    Logger.Instance.Error("Auto-connect skipped: no oscilloscope selected.");
                    _isConnectable = false;
                    UpdateCaptureModeIndicators();
                    return;
                }

                // Consider session valid only if socket says connected AND our own flag is true AND ping responds.
                bool assumeConnected = (_scope != null && _scope.IsConnected && _isConnectable);
                if (assumeConnected)
                {
                    var pingOk = await PingScopeAsync(800).ConfigureAwait(true);
                    if (!pingOk)
                    {
                        assumeConnected = false;
                        try { await _scope.DisconnectAsync(); } catch { }
                    }
                }

                if (!assumeConnected)
                {
                    // Early reachability check to avoid waiting full TCP timeout when cable is unplugged
                    var pingOk = await PingScopeAsync(800).ConfigureAwait(true);
                    if (!pingOk)
                    {
                        Logger.Instance.Error("Oscilloscope is unreachable (ICMP ping failed).");
                        _isConnectable = false;
                        SetConnectivityStatus("Oscilloscope is unreachable (ping failed)", Color.Red);
                        try { await (_scope?.DisconnectAsync() ?? Task.CompletedTask); } catch { }
                        _scope = null;
                        return;
                    }

                    try { if (_scope != null) await _scope.DisconnectAsync(); } catch { }
                    _scope = ScopeFactory.Create(vendor, modelPattern, resource);

                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();

                    Logger.Instance.Info(string.Format("Connecting to {0} {1} at {2}.", vendor, modelDisplay, resource));
                    try
                    {
                        await _scope.ConnectAsync(_cts.Token).ConfigureAwait(true);
                        Logger.Instance.Info("Network session established.");
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Instance.Error("Network session canceled.");
                        _isConnectable = false;
                        SetConnectivityStatus("Network session canceled", Color.Red);
                        try { await _scope.DisconnectAsync(); } catch { }
                        _scope = null;
                        return;
                    }
                    catch (TimeoutException tex)
                    {
                        Logger.Instance.Error("Network session cannot be established: " + InnermostMessage(tex));
                        _isConnectable = false;
                        SetConnectivityStatus("Network session cannot be established (timeout)", Color.Red);
                        try { await _scope.DisconnectAsync(); } catch { }
                        _scope = null;
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error("Network session failed: " + InnermostMessage(ex));
                        _isConnectable = false;
                        SetConnectivityStatus("Failure - oscilloscope is not connectable via network", Color.Red);
                        try { await _scope.DisconnectAsync(); } catch { }
                        _scope = null;
                        return;
                    }
                }
                else
                {
                    Logger.Instance.Info("Network session already established.");
                }

                // Mark connectable only after we can talk to the instrument (IDN sanity check)
                try
                {
                    var idn = await QueryIdentifyViaSuiteAsync().ConfigureAwait(true);
                    var parsed = ParseIdn(idn);
                    Logger.Instance.Info("Oscilloscope identified as:");
                    Logger.Instance.Info("Vendor: " + parsed.Vendor);
                    Logger.Instance.Info("Model: " + parsed.Model);
                    if (!string.IsNullOrWhiteSpace(parsed.Serial))
                        Logger.Instance.Info("Serial: " + (_config != null && _config.MaskSerial ? new string('*', (parsed.Serial ?? string.Empty).Length) : parsed.Serial));
                    Logger.Instance.Info("Firmware: " + parsed.Firmware);

                    _isConnectable = true;
                    SetConnectivityStatus("Success - you are now connected to the oscilloscope, and capture mode has been enabled", Color.Green);

                    PopulateCommandTextboxes();
                    ApplyScpiOverridesForCurrentProfile();
                }
                catch (TimeoutException tex)
                {
                    Logger.Instance.Error("Network session cannot be established: " + InnermostMessage(tex));
                    _isConnectable = false;
                    SetConnectivityStatus("Network session cannot be established (timeout)", Color.Red);

                    try { await _scope.DisconnectAsync(); } catch { }
                    _scope = null;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Connectivity check failed: " + InnermostMessage(ex));
                    _isConnectable = false;
                    SetConnectivityStatus("Failure - oscilloscope is not connectable via network", Color.Red);

                    try { await _scope.DisconnectAsync(); } catch { }
                    _scope = null;
                }
            }
            finally
            {
                UpdateCaptureModeIndicators(); // reflect latest state
            }
        }

        private void LogIdnVendorModelFirmware(string idn)
        {
            try
            {
                var parsed = ParseIdn(idn);
                if (!string.IsNullOrWhiteSpace(parsed.Vendor)) Logger.Instance.Info("Vendor: " + parsed.Vendor);
                if (!string.IsNullOrWhiteSpace(parsed.Model)) Logger.Instance.Info("Model: " + parsed.Model);
                if (!string.IsNullOrWhiteSpace(parsed.Serial)) Logger.Instance.Info("Serial: " + (_config != null && _config.MaskSerial ? new string('*', parsed.Serial.Length) : parsed.Serial));
                if (!string.IsNullOrWhiteSpace(parsed.Firmware)) Logger.Instance.Info("Firmware: " + parsed.Firmware);
            }
            catch { /* ignore parse/log failures */ }
        }

        // Add this helper near your other private helpers
        private bool IsKeyboardContextAllowed()
        {
            try
            {
                if (!_captureMode) return false;
                if (tabMain == null) return false;

                var selected = tabMain.SelectedTab;
                if (selected == null) return false;

                // Prefer designer fields; fallback to Find by name if null
                var capturingTab = tabCapturing ?? this.Controls.Find("tabCapturing", true).FirstOrDefault() as TabPage;
                var debugTab = tabDebug ?? this.Controls.Find("tabDebug", true).FirstOrDefault() as TabPage;

                // Only Capturing or Debug tabs allow hotkeys
                bool tabOk = ReferenceEquals(selected, capturingTab) || ReferenceEquals(selected, debugTab);
                if (!tabOk) return false;

                // If user is editing filename/number/variables, suspend hotkeys
                if (_suspendCaptureHotkeys) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> QueryIdentifyViaSuiteAsync()
        {
            var ct = _cts?.Token ?? System.Threading.CancellationToken.None;

            // Suite header to match test-suite logging
            Logger.Instance.Debug("---");
            Logger.Instance.Debug(Oscilloscope_Network_Capture.Core.Scopes.ScopeTestSuiteRegistry.GetDisplayName(Oscilloscope_Network_Capture.Core.Scopes.ScopeTestSuite.QueryIdentify) + ":");

            var steps = Oscilloscope_Network_Capture.Core.Scopes.ScopeTestSuiteRegistry.Resolve(_config, Oscilloscope_Network_Capture.Core.Scopes.ScopeTestSuite.QueryIdentify);
            string primary = GetPrimaryScpiForSuite(Oscilloscope_Network_Capture.Core.Scopes.ScopeTestSuite.QueryIdentify);
            string resp = null;

            foreach (var step in steps)
            {
                if (step == steps.First())
                {
                    if (IsQuery(step))
                        resp = await _scope.SendRawQueryAsync(primary, ct).ConfigureAwait(true);
                    else
                        await _scope.SendRawWriteAsync(primary, ct).ConfigureAwait(true);
                }
                else
                {
                    var scpi = GetDefaultScpiForCurrentProfile(step);
                    if (string.IsNullOrWhiteSpace(scpi)) continue;

                    if (IsQuery(step))
                        resp = await _scope.SendRawQueryAsync(scpi, ct).ConfigureAwait(true);
                    else
                        await _scope.SendRawWriteAsync(scpi, ct).ConfigureAwait(true);
                }
            }

            return resp ?? string.Empty;
        }

        // ###########################################################################################
        // Get the assembly version.
        // Will transform assembly information into a text string.
        // ###########################################################################################

        private void GetAssemblyVersion()
        {
            try
            {
                Assembly assemblyInfo = Assembly.GetExecutingAssembly();
                string assemblyVersion = FileVersionInfo.GetVersionInfo(assemblyInfo.Location).FileVersion;
                string year = assemblyVersion.Substring(0, 4);
                string month = assemblyVersion.Substring(5, 2);
                string day = assemblyVersion.Substring(8, 2);
                string rev = assemblyVersion.Substring(11); // will be ignored in RELEASE builds
                switch (month)
                {
                    case "01": month = "January"; break;
                    case "02": month = "February"; break;
                    case "03": month = "March"; break;
                    case "04": month = "April"; break;
                    case "05": month = "May"; break;
                    case "06": month = "June"; break;
                    case "07": month = "July"; break;
                    case "08": month = "August"; break;
                    case "09": month = "September"; break;
                    case "10": month = "October"; break;
                    case "11": month = "November"; break;
                    case "12": month = "December"; break;
                    default: month = "Unknown"; break;
                }
                day = day.TrimStart(new Char[] { '0' }); // remove leading zero
                day = day.TrimEnd(new Char[] { '.' }); // remove last dot
                string date = year + "-" + month + "-" + day;

                // Beautify revision and build-type 
                if (rev != "0" && rev != "1")
                {
                    rev = "(rev. " + rev + ")";
                } else
                {
                    rev = string.Empty;
                }

                // Set the application version
                versionThis = (date + " " + rev).Trim();
                labelVersion.Text = "Version: " + versionThis;

                Logger.Instance.Info("Application version is "+ versionThis);
            }
            catch {}
        }

        // Add this helper to enumerate all test buttons (now includes btnConnect)
        private IEnumerable<Button> EnumerateTestSuiteButtons()
        {
            var fields = new Button[]
            {
        btnTestIdentify, btnTestClearStats, btnTestActiveTrig, btnTestStop, btnTestSingle,
        btnTestRun, btnTestTrigMode, btnTestTrigLevelQ, btnTestTrigLevelSet,
        btnTestTimeDivQ, btnTestTimeDivSet, btnTestDumpImage, btnTestSysErr, btnTestOpc,
        btnConnect,
        _btnCaptureStart // include mirror connect button if we have it
            };

            foreach (var b in fields)
                if (b != null) yield return b;

            var names = new[]
            {
        "btnTestIdentify","btnTestClearStats","btnTestActiveTrig","btnTestStop","btnTestSingle",
        "btnTestRun","btnTestTrigMode","btnTestTrigLevelQ","btnTestTrigLevelSet",
        "btnTestTimeDivQ","btnTestTimeDivSet","btnTestDumpImage","btnTestSysErr","btnTestOpc",
        "btnConnect",
        "buttonCaptureStart","button1" // try both possible capture-start names
    };
            foreach (var n in names)
            {
                var found = this.Controls.Find(n, true).FirstOrDefault() as Button;
                if (found != null) yield return found;
            }
        }

        // Add this helper to disable other test buttons and restore their original states
        private async Task WithOtherTestButtonsDisabledAsync(Button self, Func<Task> action)
        {
            var others = EnumerateTestSuiteButtons()
                .Where(b => b != null && !ReferenceEquals(b, self))
                .Distinct()
                .ToList();

            var original = new Dictionary<Button, bool>(others.Count);
            foreach (var b in others) original[b] = b.Enabled;

            try
            {
                foreach (var b in others) b.Enabled = false;
                await action().ConfigureAwait(true);
            }
            finally
            {
                foreach (var kv in original) kv.Key.Enabled = kv.Value;
            }
        }

        // Place these helpers near other private helpers (e.g., after SetConnectButtonsEnabled)

        private void PopulateHelpTabRtf()
        {
            try
            {
                // Prefer an explicit help RichTextBox name, else find one on the Help tab, else any that looks like help.
                var rtb = this.Controls.Find("richTextBoxHelp", true).FirstOrDefault() as RichTextBox;

                if (rtb == null)
                {
                    var tabHelp = this.Controls.Find("tabHelp", true).FirstOrDefault() as TabPage;
                    if (tabHelp != null)
                        rtb = tabHelp.Controls.OfType<RichTextBox>().FirstOrDefault();

                    if (rtb == null)
                        rtb = this.Controls.OfType<RichTextBox>()
                                .FirstOrDefault(x => (x.Name ?? string.Empty).IndexOf("help", StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (rtb != null)
                {
                    rtb.Rtf = BuildHelpRtf();
                }
            }
            catch { /* best-effort */ }
        }

        private static string BuildHelpRtf()
        {
            var sb = new StringBuilder();

            // RTF header with font and color tables
            sb.Append(@"{\rtf1\ansi\deff0");
            sb.Append(@"{\fonttbl{\f0 Segoe UI;}{\f1 Consolas;}}");
            // colortbl: [0]=auto; [1]=text (black); [2]=keycap bg (light gray)
            sb.Append(@"{\colortbl ;\red0\green0\blue0;\red230\green234\blue238;}");
            sb.Append(@"\fs22 "); // 11pt base size

            sb.Append(@"{\fs28{\b Expected behaviour and how-to-use}}\line ");
            sb.Append(@"When you launch the application it will try and connect to the oscilloscope. If this is successful, then it has started its ""capture mode"", where you can control your oscilloscope and save an image from it. However, the capture mode will only be active and working, when you are within one of the two tabs:\line ");
            sb.Append(@"\line");
            sb.Append(@"    * Capturing\line ");
            sb.Append(@"    * Debug\line ");
            sb.Append(@"\line ");
            sb.Append(@"If you are not in capture mode (meaning, if you are not within any of these two tabs), then you can hit "+ KeycapRtf("ENTER") + @" or "+ KeycapRtf("DELETE") +@", without it saving or deleting any files. It will work very nicely with an external numpad or keyboard next to your oscilloscope, so you can focus specifically on the oscilloscope and your measurement only (one hand at numpad, and one hand for scope measurement). View below for keyboard controls for the oscilloscope.\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Standard and protocol used:}}\line ");
            sb.Append(@"    * IEEE 488.2 standard\line ");
            sb.Append(@"    * SCPI socket protocol\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Variables to use in filename format}}\line ");
            sb.Append(@"    * " + KeycapRtf("{NUMBER}") + @" is a sequential number counting up for every measurement you do (e.g. it could be IC pin number 7)\line ");
            sb.Append(@"    * " + KeycapRtf("{COMPONENT}") + @" is the component you are measuring on\line ");
            sb.Append(@"    * " + KeycapRtf("{VAR2}") + @" is some text or number you define yourself - you can have up to 5 variables\line ");
            sb.Append(@"    * " + KeycapRtf("{DATE}") + @" is YYYYMMDD - e.g. 20251231\line ");
            sb.Append(@"    * " + KeycapRtf("{TIME}") + @" is HHMMSS - e.g. 235959\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b General}}\line ");
            sb.Append(@"You can always change the value of a variable on-the-fly, and it will be used for the next saved file.\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Keyboard commands (only in capture mode)}}\line ");
            sb.Append("    * " + KeycapRtf("ENTER") + " will save image from scope to a file with a specific filename format, and increase the "+ KeycapRtf("{NUMBER}") + @"\line ");
            sb.Append("    * " + KeycapRtf("DELETE") + " will delete last saved file; requires a checkbox to be set in \"Settings\" \\line ");
            sb.Append("    * " + KeycapRtf("*") + " to set SINGLE trigger on scope\\line ");
            sb.Append("    * " + KeycapRtf("/") + " to RESUME acquisition on scope\\line ");
            sb.Append("    * " + KeycapRtf("NUMPAD DECIMAL") + " to \"Clear Statistics\" on scope\\line ");
            sb.Append("    * " + KeycapRtf("+") + " to decrease TIME/DIV timespan (zoom-in)\\line ");
            sb.Append("    * " + KeycapRtf("-") + " to increase TIME/DIV timespan (zoom-out)\\line ");
            sb.Append("    * " + KeycapRtf("ARROW UP") + " to raise trigger level\\line ");
            sb.Append("    * " + KeycapRtf("ARROW DOWN") + " to lower trigger level\\line ");
            sb.Append("    * " + KeycapRtf("NUMPAD 9") + " is a \"secret\" command to set scope to "+ KeycapRtf("1uS") + " as a quick (experimental) reference\\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Checkboxes}}\line ");
            sb.Append("    * Enable beep at capturing\\line ");
            sb.Append("        - Will give a subtle beep before and after capturing an image\\line ");
            sb.Append("    * Force acquisition after capture\\line ");
            sb.Append("        - Even if scope was put in STOP mode at capture time, then it will be forced in to RUN mode again\\line ");
            sb.Append("    * Force \"Clear Statistics\" before capture\\line ");
            sb.Append("        - Before capturing image, then it will clear scope statistics\\line ");
            sb.Append("    * Delay in milliseconds to settle on new statistics (0-5000)\\line ");
            sb.Append("        - After clearing the statistics, then give it some time to adjust the numbers before capturing\\line ");
            sb.Append("    * Snap-to-grid for trigger level\\line ");
            sb.Append("        - The trigger level voltage change, and then snap to nearest \"snap-level\"\\line ");
            sb.Append("    * Keyboard [DELETE] will delete last saved file\\line ");
            sb.Append("        - When checked you will be able to delete the last saved file\\line ");
            sb.Append("        - It is just an awareness for you, to ensure you do not inadvertently delete a file by mistake\\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Troubleshoot no connectivity to you oscilloscope}}\line ");
            sb.Append(@"Make sure the IP address and port is open in your firewall.\line\line ");
            sb.Append(@"Also, sometimes it helps power-recycling the scope, if some wrong commands have been sent.\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Special consideration for a couple of SET commands}}\line ");
            sb.Append("    * Set trigger level\\line ");
            sb.Append("        - Keep "+ KeycapRtf("{0}") +" as the value to pass - it will inherit value from \"Query trigger level\", so you cannot manually set a value\\line ");
            sb.Append("    * Set TIME/DIV\\line ");
            sb.Append("        - Keep " + KeycapRtf("{0}") + " as the value to pass - it will inherit value from \"Query TIME/DIV\", so you cannot manually set a value\\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b How you can help to get your oscilloscope supported in tool}}\line ");
            sb.Append(@"Find the SCP command reference for your oscilloscope, and tweak the test-suites in ""Configuration"" until they are all correct and works.\line ");
            sb.Append(@"When increasing and decreasing TIME/DIV or trigger level, then make sure the scope does not ""tilt"" and go over-range, so make sure the values and notations are correct.\line ");
            sb.Append(@"When everything is working as expected, then follow this simple little guide:\line");
            sb.Append(@"\line");
            sb.Append("    * Launch the application\\line ");
            sb.Append("    * Go through each test-suite in \"Configuration\"\\line ");
            sb.Append("    * Go to the \"Debug\" tab, and press the button \"Send debug info and configuration file to developer\"\\line ");
            sb.Append("        - I would appreciate if you fill in your email, so I can connect with you for any questions I may have :-)\\line ");

            sb.Append(@"\line");

            sb.Append("}");

            return sb.ToString();
        }

        private static string BuildAboutRtf()
        {
            var sb = new StringBuilder();

            // RTF header with font table (same style as BuildHelpRtf)
            sb.Append(@"{\rtf1\ansi\deff0");
            sb.Append(@"{\fonttbl{\f0 Segoe UI;}{\f1 Consolas;}}");
            sb.Append(@"{\colortbl ;\red0\green0\blue0;}");
            sb.Append(@"\fs22 "); // 11pt base size

            sb.Append(@"{\i ONC}{\i0} is a simple application, which has been originally designed for one purpose only:\line ");
            sb.Append(@"\line ");

            sb.Append(@"{""\i Make it easier and faster for me to capture oscilloscope measurements, when creating oscilloscope baseline images for my other project; Commodore Repair Toolbox.}{\i0}""\line ");
            sb.Append(@"\line ");

            sb.Append(@"It has been designed to work specifically with my Rigol DS2202A oscilloscope, but as it uses the SCPI socket protocol, then it should also work for other oscilloscopes also, if the configured SCPI commands are correct.\line ");
            sb.Append(@"\line ");

            sb.Append(@"The goal is not to make this a full-blown ""Swiss army knife"" that suits everyones need, but if can help someone other than myself, then I am happy to make this available. For sure I have made it much more robust and user-friendly than my first rough PoC :-)\line ");
            sb.Append(@"\line ");

            sb.Append(@"You can check for a newer version on its official GitHub project page, https://github.com/HovKlan-DH/Oscilloscope-Network-Capture\line ");
            sb.Append(@"\line ");

            sb.Append(@"Kind regards,\line ");
            sb.Append(@"\line ");

            sb.Append(@"Dennis Helligsø, dennis@commodore-repair-toolbox.dk\line ");

            sb.Append("}");
            return sb.ToString();
        }

        private static string KeycapRtf(string text)
        {
            // base font for keys a tad smaller than body
            var inner = $"\\f0\\fs20\\cf1\\highlight2\\~{EscapeRtf(text)}\\~\\highlight0";
            return "{" + inner + "}";
        }

        private static string EscapeRtf(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
        }

        private void PopulateAboutTabRtf()
        {
            try
            {
                // Prefer an explicit about RichTextBox name, else find one on the About tab, else any that looks like "about".
                var rtb = this.Controls.Find("richTextBoxAbout", true).FirstOrDefault() as RichTextBox;

                if (rtb == null)
                {
                    var tabAbout = this.Controls.Find("tabAbout", true).FirstOrDefault() as TabPage;
                    if (tabAbout != null)
                        rtb = tabAbout.Controls.OfType<RichTextBox>().FirstOrDefault();

                    if (rtb == null)
                        rtb = this.Controls.OfType<RichTextBox>()
                                .FirstOrDefault(x => (x.Name ?? string.Empty).IndexOf("about", StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (rtb != null)
                {
                    rtb.Rtf = BuildAboutRtf();
                }
            }
            catch { /* best-effort */ }
        }

        // Add near other private helpers
        private void WireRichTextBoxHyperlinks(params string[] names)
        {
            foreach (var n in names ?? Array.Empty<string>())
            {
                var rtb = this.Controls.Find(n, true).FirstOrDefault() as RichTextBox;
                if (rtb == null) continue;

                // Ensure auto-link detection and hook handler
                rtb.DetectUrls = true;

                // Avoid multiple subscriptions
                rtb.LinkClicked -= RichTextBox_LinkClicked;
                rtb.LinkClicked += RichTextBox_LinkClicked;

                // Re-trigger URL auto-detection after assigning RTF
                // (RichTextBox sometimes needs a toggle to detect links in RTF)
                rtb.DetectUrls = false;
                rtb.DetectUrls = true;
            }
        }

        private void RichTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                // UseShellExecute ensures default browser is used
                Process.Start(new ProcessStartInfo(e.LinkText) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Failed to open link: " + InnermostMessage(ex));
                try { MessageBox.Show("Unable to open link:\r\n" + e.LinkText, "Open Link", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
            }
        }

        private void removeFocus ()
        {
            // Remove caret from any input control and ensure form receives keys
            this.ActiveControl = null;
            this.Select();

            // Resume hotkeys and refresh indicators
            _suspendCaptureHotkeys = false;
            UpdateCaptureModeIndicators();
        }

        private void FlashEnterFeedback(int durationMs = 100)
        {
            try
            {
                if (picScreen == null) return;

                if (_enterFlashOverlay == null)
                {
                    _enterFlashOverlay = new Panel
                    {
                        BackColor = Color.DimGray,   // brief solid grey flash
                        Visible = false,
                        Enabled = false
                    };
                    _enterFlashOverlay.Parent = picScreen;
                    _enterFlashOverlay.Bounds = new Rectangle(0, 0, picScreen.ClientSize.Width, picScreen.ClientSize.Height);
                    _enterFlashOverlay.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                }

                // Ensure it covers current size
                _enterFlashOverlay.BringToFront();
                _enterFlashOverlay.Visible = true;

                if (_enterFlashTimer == null)
                {
                    _enterFlashTimer = new System.Windows.Forms.Timer();
                    _enterFlashTimer.Tick += (s, a) =>
                    {
                        _enterFlashTimer.Stop();
                        if (_enterFlashOverlay != null) _enterFlashOverlay.Visible = false;
                    };
                }

                _enterFlashTimer.Interval = Math.Max(40, durationMs);
                _enterFlashTimer.Stop();
                _enterFlashTimer.Start();
            }
            catch { /* best-effort */ }
        }

        // Call this whenever capture mode might change
        // Use the explicit flag in the indicators
        private void UpdateCaptureModeIndicators()
        {
            try
            {
                var lblActive = this.labelCaptureModeActive
                                ?? this.Controls.Find("labelCaptureModeActive", true).FirstOrDefault() as Label;
                var lblInactive = this.labelCaptureModeInactive
                                  ?? this.Controls.Find("labelCaptureModeInactive", true).FirstOrDefault() as Label;

                bool captureMode = _captureMode;
                bool hotkeysSuspended = _suspendCaptureHotkeys;
                bool activeUi = captureMode && !hotkeysSuspended;

                // IMPORTANT: use our own connectable flag, not _scope?.IsConnected
                bool scopeConnected = _isConnectable;

                const string InactiveWhileSuspendedText =
                    "Capture mode inactive.\r\n\r\nClick outside input field\r\nto reactivate capture mode.";
                const string InactiveWhileOffButConnectedText =
                    "Capture mode inactive.\r\n\r\nClick outside input field\r\nto reactivate capture mode.";
                const string InactiveWhileOffAndNotConnectedText =
                    "Capture mode inactive and\r\noscilloscope is not connected.\r\n\r\nClick the \"Connect to oscilloscope\" button.";

                void Apply()
                {
                    if (lblActive != null) lblActive.Visible = activeUi;

                    if (lblInactive != null)
                    {
                        lblInactive.Visible = !activeUi;

                        if (captureMode && hotkeysSuspended)
                        {
                            lblInactive.Text = InactiveWhileSuspendedText;
                        }
                        else if (!captureMode && scopeConnected)
                        {
                            lblInactive.Text = InactiveWhileOffButConnectedText;
                        }
                        else
                        {
                            lblInactive.Text = InactiveWhileOffAndNotConnectedText;
                        }
                    }
                }

                if (this.IsHandleCreated && this.InvokeRequired)
                    this.BeginInvoke(new Action(Apply));
                else
                    Apply();
            }
            catch { }
        }

        // Add these helpers near other private helpers (e.g., close to UpdateCaptureModeIndicators)

        private void EnsureConnectionMonitorStarted()
        {
            try
            {
                if (_connectionMonitorTimer == null)
                {
                    _connectionMonitorTimer = new System.Windows.Forms.Timer();
                    _connectionMonitorTimer.Interval = 1500; // nominal heartbeat
                    _connectionMonitorTimer.Tick += async (s, a) => await ConnectionMonitorTickAsync();
                }

                if (_captureMode)
                {
                    // Reset streak when (re)starting monitoring
                    _connFailStreak = 0;
                    _lastHeartbeatUtc = DateTime.UtcNow;

                    _connectionMonitorTimer.Stop();
                    _connectionMonitorTimer.Start();
                }
            }
            catch { /* best-effort */ }
        }

        private void StopConnectionMonitor()
        {
            try { _connectionMonitorTimer?.Stop(); } catch { }
        }

        private async Task ConnectionMonitorTickAsync()
        {
            // Stop monitoring if capture mode was turned off
            if (!_captureMode)
            {
                StopConnectionMonitor();
                return;
            }

            // Prevent overlapping checks
            if (System.Threading.Interlocked.CompareExchange(ref _connectionCheckBusy, 1, 0) != 0)
                return;

            try
            {
                // If a capture-mode key action is running, skip this tick to avoid stepping on user ops
                if (System.Threading.Volatile.Read(ref _keyActionBusy) != 0)
                    return;

                var scope = _scope;
                if (scope == null || !scope.IsConnected)
                {
                    // Treat explicit disconnect as hard loss
                    _connFailStreak = ConnectionFailThreshold;
                }
                else
                {
                    bool ok = await PingScopeAsync(1000).ConfigureAwait(true);

                    if (ok)
                    {
                        _connFailStreak = 0;
                        _lastHeartbeatUtc = DateTime.UtcNow;

                        // Return monitor interval to normal after previous failures
                        if (_connectionMonitorTimer != null && _connectionMonitorTimer.Interval != 1500)
                            _connectionMonitorTimer.Interval = 1500;
                    }
                    else
                    {
                        // Count the failure; bias for quick recheck on next tick
                        _connFailStreak++;
                        if (_connectionMonitorTimer != null && _connectionMonitorTimer.Interval > 800)
                            _connectionMonitorTimer.Interval = 800; // temporarily recheck faster
                    }
                }

                if (_connFailStreak >= ConnectionFailThreshold)
                {
                    OnConnectionLost("Oscilloscope connection lost (ICMP ping failed).");
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _connectionCheckBusy, 0);
            }
        }

        // Ensure we clear the flag when connection is lost
        private void OnConnectionLost(string reason)
        {
            try
            {
                StopConnectionMonitor();
                _isConnectable = false;            // <-- clear on loss
                DisableCaptureMode(reason);
                SetConnectivityStatus("Connection lost - capture mode disabled", Color.Red);
                Logger.Instance.Error(reason ?? "Connection lost.");
            }
            catch { }
            finally
            {
                UpdateCaptureModeIndicators();
            }
        }

        // Centralized way to exit capture mode (used by monitor and can be reused elsewhere)
        private void DisableCaptureMode(string reason = null)
        {
            if (!_captureMode) return;

            _captureMode = false;
            try { this.KeyDown -= Form1_Capture_KeyDown; } catch { }

            UpdateCaptureModeIndicators();

            if (!string.IsNullOrWhiteSpace(reason))
            {
                try { Logger.Instance.Info("Capture mode disabled: " + reason); } catch { }
            }
        }

        private async Task<bool> PingScopeAsync(int timeoutMs = 800)
        {
            try
            {
                var ip = (txtIp?.Text ?? _config?.ScopeIp ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(ip))
                    return false;

                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ip, Math.Max(100, timeoutMs));
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
