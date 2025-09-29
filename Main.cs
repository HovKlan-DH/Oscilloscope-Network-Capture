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

        private int _timeDivAdjustBusy;

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
        private bool _captureMode;
        private Button _btnCaptureStart;
        private TextBox _tbCaptureFolder;
        private TextBox _tbFilenameFormat;

        public Main()
        {
            InitializeComponent();
            Logger.Instance.MessageLogged += Logger_MessageLogged;
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
//                case LogLevel.Warn:  return Color.OrangeRed;      // requested change
                case LogLevel.Error: return Color.LightPink;      // requested
                default:             return Color.Gainsboro;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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
                _config.NumberValue = "0";
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
                ConfigurationService.Save(_config);
            };

            cboModel.SelectedIndexChanged += (s, a) =>
            {
                var modelDisplay = cboModel.SelectedItem as string ?? string.Empty;
                _config.Model = ToModelPattern(modelDisplay); // e.g., "Generic" => "*"
                PopulateCommandTextboxes();        // refresh defaults for the new model
                ApplyScpiOverridesForCurrentProfile();
                UpdateScpiHeaderLabel();
                ConfigurationService.Save(_config);
            };

            // Then do the initial populate:
            UpdateModelsForVendor();
            PopulateCommandTextboxes();
            ApplyScpiOverridesForCurrentProfile();
            UpdateScpiHeaderLabel();

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

            // Initialize Measurements NUMBER from config
            if (numericUpDown1 != null)
            {
                decimal parsed;
                if (decimal.TryParse(_config.NumberValue ?? "0", NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
                {
                    if (parsed >= numericUpDown1.Minimum && parsed <= numericUpDown1.Maximum)
                        numericUpDown1.Value = parsed;
                }
                numericUpDown1.ValueChanged += (s, a) =>
                {
                    _config.NumberValue = numericUpDown1.Value.ToString(CultureInfo.InvariantCulture);
                    ConfigurationService.Save(_config);
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

                // NEW: sync Delay numeric enabled state on load
                if (numDelay != null) numDelay.Enabled = chkForceClear.Checked;

                chkForceClear.CheckedChanged += (s, a) =>
                {
                    _config.ForceClear = chkForceClear.Checked;
                    ConfigurationService.Save(_config);

                    // NEW: toggle Delay numeric when the checkbox changes
                    if (numDelay != null) numDelay.Enabled = chkForceClear.Checked;
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
            if (this.Controls.Find("button2", true).FirstOrDefault() is Button btnOpenCapture)
            {
                btnOpenCapture.Click += (s, a) => OpenCaptureFolder();
            }

            // Wire capture start button (prefer new name, fallback to legacy button1)
            _btnCaptureStart = this.Controls.Find("buttonCaptureStart", true).FirstOrDefault() as Button
                               ?? this.Controls.Find("button1", true).FirstOrDefault() as Button;
            if (_btnCaptureStart != null)
            {
                _btnCaptureStart.Click += async (s, a) => await StartCaptureModeAsync();
            }

            // Enable layout persistence after initial show
            _suppressLayoutSave = false;

            if (tabConfigMisc != null)
            {
                tabConfigMisc.AutoScroll = true;
            }

            // Ensure all version badges are placed top-right within their tab
            ApplyTopRightBadges();

            // Initialize action text with current next filename
            UpdateActionRichTextForNextFilename();

            // Persist/restore Adjust-to-grid combobox
            WireAdjustToGridComboPersistence();

            // Kick off online version check (non-blocking)
            BeginInvoke(new Action(async () =>
            {
                try { await CheckForNewVersionAsync(); } catch { }
            }));

            // Auto-connect on startup if a config file already existed (not first run)
            if (_hadConfigOnStartup)
            {
                BeginInvoke(new Action(async () =>
                {
                    try { await ConnectAndRefreshAsync(); } catch { }
                }));
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

        private string GetDefaultScpiForCurrentProfile(ScopeCommand cmd)
        {
            var vendor = cboVendor.SelectedItem as string;
            var modelDisplay = cboModel.SelectedItem as string;
            var model = ToModelPattern(modelDisplay);
            var profSpecific = ScpiProfileRegistry.Find(vendor, model);
            var profDefault = ScpiProfileRegistry.Find(vendor, "*");
            string scpi;
            if (profSpecific != null && profSpecific.TryGet(cmd, out scpi)) return scpi;
            if (profDefault != null && profDefault.TryGet(cmd, out scpi)) return scpi;
            return string.Empty;
        }

        private void ApplyScpiOverridesForCurrentProfile()
        {
            var vendor = cboVendor.SelectedItem as string ?? string.Empty;
            var modelDisplay = cboModel.SelectedItem as string ?? string.Empty;
            var model = ToModelPattern(modelDisplay);
            var prof = _config.ScpiProfiles.FirstOrDefault(p => string.Equals(p.Vendor, vendor, StringComparison.OrdinalIgnoreCase) && string.Equals(p.Model, model, StringComparison.OrdinalIgnoreCase));
            if (prof == null) return;
            string GetVal(string cmd)
            {
                var v = prof.Overrides.FirstOrDefault(o => string.Equals(o.Command, cmd, StringComparison.OrdinalIgnoreCase))?.Value;
                return string.IsNullOrWhiteSpace(v) ? null : v;
            }
            txtCmdIdentify.Text = GetVal(nameof(ScopeCommand.Identify)) ?? txtCmdIdentify.Text;
            txtCmdClearStats.Text = GetVal(nameof(ScopeCommand.ClearStatistics)) ?? txtCmdClearStats.Text;
            txtCmdActiveTrig.Text = GetVal(nameof(ScopeCommand.QueryActiveTrigger)) ?? txtCmdActiveTrig.Text;
            txtCmdStop.Text = GetVal(nameof(ScopeCommand.Stop)) ?? txtCmdStop.Text;
            txtCmdRun.Text = GetVal(nameof(ScopeCommand.Run)) ?? txtCmdRun.Text;
            txtCmdSingle.Text = GetVal(nameof(ScopeCommand.Single)) ?? txtCmdSingle.Text;
            txtCmdTrigMode.Text = GetVal(nameof(ScopeCommand.QueryTriggerMode)) ?? txtCmdTrigMode.Text;
            txtCmdTrigLevelQ.Text = GetVal(nameof(ScopeCommand.QueryTriggerLevel)) ?? txtCmdTrigLevelQ.Text;
            txtCmdTrigLevelSet.Text = GetVal(nameof(ScopeCommand.SetTriggerLevel)) ?? txtCmdTrigLevelSet.Text;
            txtCmdTimeDivQ.Text = GetVal(nameof(ScopeCommand.QueryTimeDiv)) ?? txtCmdTimeDivQ.Text;
            txtCmdTimeDivSet.Text = GetVal(nameof(ScopeCommand.SetTimeDiv)) ?? txtCmdTimeDivSet.Text;
            txtCmdDumpImage.Text = GetVal(nameof(ScopeCommand.DumpImage)) ?? txtCmdDumpImage.Text;
            txtCmdSysErr.Text = GetVal(nameof(ScopeCommand.PopLastSystemError)) ?? txtCmdSysErr.Text;
            txtCmdOpc.Text = GetVal(nameof(ScopeCommand.OperationComplete)) ?? txtCmdOpc.Text;
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


        private void PopulateCommandTextboxes()
        {
            var vendor = cboVendor.SelectedItem as string;
            var modelDisplay = cboModel.SelectedItem as string;
            var model = ToModelPattern(modelDisplay);
            var profSpecific = ScpiProfileRegistry.Find(vendor, model);
            var profDefault = ScpiProfileRegistry.Find(vendor, "*");

            string GetCmd(ScopeCommand cmd)
            {
                string scpi;
                if (profSpecific != null && profSpecific.TryGet(cmd, out scpi)) return scpi;
                if (profDefault != null && profDefault.TryGet(cmd, out scpi)) return scpi;
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
            txtCmdSysErr.Text = GetCmd(ScopeCommand.PopLastSystemError);
            txtCmdOpc.Text = GetCmd(ScopeCommand.OperationComplete);
        }

        private void UpdateModelsForVendor()
        {
            var vendor = cboVendor.SelectedItem as string;
            var driverModels = ScopeFactory.GetAvailableScopes().Where(d => d.Vendor.Equals(vendor ?? string.Empty, StringComparison.OrdinalIgnoreCase)).Select(d => d.Model);
            var profileModels = ScpiProfileRegistry.Profiles.Where(p => p.Vendor.Equals(vendor ?? string.Empty, StringComparison.OrdinalIgnoreCase)).Select(p => p.ModelPattern);

            var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in driverModels) models.Add(ToDisplayModel(m));
            foreach (var m in profileModels) models.Add(ToDisplayModel(m));

            var ordered = models
                .OrderBy(m => string.Equals(m, "Generic", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(m => m, StringComparer.OrdinalIgnoreCase)
                .ToList();

            cboModel.Items.Clear();
            cboModel.Items.AddRange(ordered.Cast<object>().ToArray());

            var lastModelDisplay = ToDisplayModel(_config?.Model ?? string.Empty);
            if (!string.IsNullOrEmpty(lastModelDisplay) && ordered.Any(x => string.Equals(x, lastModelDisplay, StringComparison.OrdinalIgnoreCase)))
                cboModel.SelectedItem = ordered.First(x => string.Equals(x, lastModelDisplay, StringComparison.OrdinalIgnoreCase));
            else if (ordered.Count > 0)
                cboModel.SelectedIndex = 0;
        }

        private int GetDefaultPortForVendor(string vendor)
        {
            if (string.IsNullOrEmpty(vendor)) return 5025;
            switch (vendor.Trim())
            {
                case "Rigol": return 5555;
                case "Siglent": return 5025;
                case "Keysight": return 5025;
                case "Agilent": return 5025;
                case "Rohde & Schwarz": return 5025;
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

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                btnConnect.Enabled = false;
                await ConnectAndRefreshAsync();
            }
            finally
            {
                btnConnect.Enabled = true;
                if (btnConnect.CanFocus) btnConnect.Focus();
            }
        }

        // Save per-profile override at time of testing — only if different from default
        private void SaveOverrideOnTestClick(ScopeCommand cmd, String value)
        {
            var defaultScpi = (GetDefaultScpiForCurrentProfile(cmd) ?? string.Empty).Trim();
            var current = (value ?? string.Empty).Trim();

            var prof = FindCurrentProfileOverride();
            var existing = prof?.Overrides?.FirstOrDefault(o => string.Equals(o.Command, cmd.ToString(), StringComparison.OrdinalIgnoreCase));

            // If matches default or empty -> remove override (if any) and possibly remove empty profile node
            if (string.Equals(current, defaultScpi, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(current))
            {
                if (existing != null)
                {
                    prof.Overrides.Remove(existing);
                }
                if (prof != null && (prof.Overrides == null || prof.Overrides.Count == 0))
                {
                    _config.ScpiProfiles.Remove(prof);
                }
                ConfigurationService.Save(_config);
                return;
            }

            // Different from default: ensure override exists and update
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
                prof.Overrides.Add(new ScpiCommandOverride { Command = cmd.ToString(), Value = value });
            }
            else
            {
                existing.Value = value;
            }
            ConfigurationService.Save(_config);
        }

        // Test button handlers (wired in Designer) — updated to save overrides first
        private async void btnTestIdentify_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.Identify, txtCmdIdentify.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusIdentify, async () =>
            {
                await RunSuiteAsync(ScopeTestSuite.QueryIdentify, txtCmdIdentify.Text, lblStatusIdentify);
            });
        }
        private async void btnTestClearStats_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.ClearStatistics, txtCmdClearStats.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusClearStats, async () =>
            {
                await RunSuiteAsync(ScopeTestSuite.ClearStatistics, txtCmdClearStats.Text, lblStatusClearStats);
            });
        }
        private async void btnTestActiveTrig_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.QueryActiveTrigger, txtCmdActiveTrig.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusActiveTrig, async () =>
            {
                await RunSuiteAsync(ScopeTestSuite.QueryActiveTrigger, txtCmdActiveTrig.Text, lblStatusActiveTrig);
            });
        }
        private async void btnTestStop_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.Stop, txtCmdStop.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusStop, async () =>
            {
                await RunSuiteAsync(ScopeTestSuite.Stop, txtCmdStop.Text, lblStatusStop);
            });
        }
        private async void btnTestRun_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.Run, txtCmdRun.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusRun, async () =>
            {
                await RunSuiteAsync(ScopeTestSuite.Run, txtCmdRun.Text, lblStatusRun);
            });
        }
        private async void btnTestSingle_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.Single, txtCmdSingle.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusSingle, async () =>
            {
                await RunSuiteAsync(ScopeTestSuite.Single, txtCmdSingle.Text, lblStatusSingle);
            });
        }
        private async void btnTestTrigMode_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.QueryTriggerMode, txtCmdTrigMode.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusTrigMode, async () =>
            {
                await RunSuiteAsync(ScopeTestSuite.QueryTriggerMode, txtCmdTrigMode.Text, lblStatusTrigMode);
            });
        }
        private async void btnTestTrigLevelQ_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.QueryTriggerLevel, txtCmdTrigLevelQ.Text);

            await WithButtonDisabledAsync((Button)sender, lblStatusTrigLevelQ, async () =>
            {
                try
                {
                    await EnsureConnectedAsync();
                    var ct = _cts?.Token ?? CancellationToken.None;

                    // Use the suite that honors profile/overrides and returns a parsed double
                    double current = await QueryTriggerLevelViaSuiteAsync().ConfigureAwait(true);

                    // DO NOT mutate the Set textbox; keep its {0} so it remains parameterized
                    // If you want a preview, consider showing it in a label/tooltip instead.
                    // var setCmd = txtCmdTrigLevelSet?.Text ?? string.Empty;
                    // var valStr = current.ToString("0.######", CultureInfo.InvariantCulture);
                    // if (!string.IsNullOrWhiteSpace(setCmd) && setCmd.IndexOf("{0}", StringComparison.Ordinal) >= 0)
                    // {
                    //     txtCmdTrigLevelSet.Text = setCmd.Replace("{0}", valStr);
                    // }

                    // Cache in the Set button and enable it
                    if (btnTestTrigLevelSet != null)
                    {
                        btnTestTrigLevelSet.Tag = current; // cached value
                        btnTestTrigLevelSet.Enabled = true;
                        if (lblStatusTrigLevelSet.Text == "Not tested - first run \"Query trigger level\"")
                        {
                            lblStatusTrigLevelSet.Text = "Not tested";
                        }
                    }

                    // Keep label text simple (do not include the value)
                    lblStatusTrigLevelQ.Text = "OK";
                    lblStatusTrigLevelQ.ForeColor = Color.Green;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("QueryTriggerLevel test failed: " + InnermostMessage(ex));
                    lblStatusTrigLevelQ.Text = "Fail (" + InnermostMessage(ex) + ")";
                    lblStatusTrigLevelQ.ForeColor = Color.Red;

                    // Keep Set disabled on failure
                    if (btnTestTrigLevelSet != null)
                    {
                        btnTestTrigLevelSet.Tag = null;
                        btnTestTrigLevelSet.Enabled = false;
                    }
                }
            });
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
        private async void btnTestTrigLevelSet_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.SetTriggerLevel, txtCmdTrigLevelSet.Text);

            await WithButtonDisabledAsync((Button)sender, lblStatusTrigLevelSet, async () =>
            {
                try
                {
                    await EnsureConnectedAsync();
                    var ct = _cts?.Token ?? CancellationToken.None;

                    // Prefer the value cached by the Query button; fallback to a live query if missing
                    double target;
                    if (btnTestTrigLevelSet != null && btnTestTrigLevelSet.Tag is double d)
                        target = d;
                    else
                        target = await _scope.QueryTriggerLevelAsync(ct).ConfigureAwait(true);

                    // Formats and runs the suite
                    await RunSetTriggerLevelSuiteAsync(target).ConfigureAwait(true);

                    // Keep label as-is, without embedding the value
                    lblStatusTrigLevelSet.Text = "OK";
                    lblStatusTrigLevelSet.ForeColor = Color.Green;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("SetTriggerLevel test failed: " + InnermostMessage(ex));
                    lblStatusTrigLevelSet.Text = "Fail (" + InnermostMessage(ex) + ")";
                    lblStatusTrigLevelSet.ForeColor = Color.Red;
                }
            });
        }
        private async void btnTestTimeDivQ_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.QueryTimeDiv, txtCmdTimeDivQ.Text);

            await WithButtonDisabledAsync((Button)sender, lblStatusTimeDivQ, async () =>
            {
                try
                {
                    await EnsureConnectedAsync();
                    var ct = _cts?.Token ?? CancellationToken.None;

                    // Query current TIME/DIV via suite (parses to seconds/div)
                    double seconds = await QueryTimeDivViaSuiteAsync().ConfigureAwait(true);

                    // IMPORTANT: Do NOT mutate the Set textbox; keep it parameterized with {0}
                    // This avoids stale or compounded numeric formatting issues when adjusting via +/- keys.

                    // Cache in the Set button and enable it
                    if (btnTestTimeDivSet != null)
                    {
                        btnTestTimeDivSet.Tag = seconds;
                        btnTestTimeDivSet.Enabled = true;
                        if (lblStatusTimeDivSet.Text == "Not tested - first run \"Query TIME/DIV\"")
                        {
                            lblStatusTimeDivSet.Text = "Not tested";
                        }
                    }

                    // Keep label simple
                    lblStatusTimeDivQ.Text = "OK";
                    lblStatusTimeDivQ.ForeColor = Color.Green;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("QueryTimeDiv test failed: " + InnermostMessage(ex));
                    lblStatusTimeDivQ.Text = "Fail (" + InnermostMessage(ex) + ")";
                    lblStatusTimeDivQ.ForeColor = Color.Red;

                    // Keep Set disabled on failure
                    if (btnTestTimeDivSet != null)
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
                try
                {
                    await EnsureConnectedAsync();

                    // Prefer the value cached by the Query button; fallback to a live query if missing
                    double targetSeconds;
                    if (btnTestTimeDivSet != null && btnTestTimeDivSet.Tag is double s)
                        targetSeconds = s;
                    else
                        targetSeconds = await QueryTimeDivViaSuiteAsync().ConfigureAwait(true);

                    // Formats {0} (if present) and runs the full suite (OPC/SysErr, etc.)
                    await RunSetTimeDivSuiteAsync(targetSeconds).ConfigureAwait(true);

                    // Keep label simple
                    lblStatusTimeDivSet.Text = "OK";
                    lblStatusTimeDivSet.ForeColor = Color.Green;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("SetTimeDiv test failed: " + InnermostMessage(ex));
                    lblStatusTimeDivSet.Text = "Fail (" + InnermostMessage(ex) + ")";
                    lblStatusTimeDivSet.ForeColor = Color.Red;
                }
            });
        }
        private async void btnTestSysErr_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.PopLastSystemError, txtCmdSysErr.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusSysErr, async () =>
            {
                await RunSuiteAsync(ScopeTestSuite.PopLastSystemError, txtCmdSysErr.Text, lblStatusSysErr);
            });
        }
        private async void btnTestOpc_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.OperationComplete, txtCmdOpc.Text);
            await WithButtonDisabledAsync((Button)sender, lblStatusOpc, async () =>
            {
                await RunSuiteAsync(ScopeTestSuite.OperationComplete, txtCmdOpc.Text, lblStatusOpc);
            });
        }

        /*
        private void numericUpDown1_ValueChanged(object sender, EventArgs e) { }
        */

        /*
        private void label2_Click(object sender, EventArgs e) { }
        */

        private async void btnTestDumpImage_Click(object sender, EventArgs e)
        {
            SaveOverrideOnTestClick(ScopeCommand.DumpImage, txtCmdDumpImage.Text);

            await WithButtonDisabledAsync((Button)sender, lblStatusDumpImage, async () =>
            {
                try
                {
                    await EnsureConnectedAsync();

                    // Log suite header
                    Logger.Instance.Debug("---");
                    Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.DumpImage) + ":");

                    // Execute pre-steps (all steps except DumpImage) using profile defaults
                    var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.DumpImage);
                    foreach (var step in steps)
                    {
                        if (step == ScopeCommand.DumpImage) continue; // we'll handle it explicitly below

                        if (IsQuery(step))
                        {
                            var scpi = GetDefaultScpiForCurrentProfile(step);
                            await _scope.SendRawQueryAsync(scpi, _cts?.Token ?? System.Threading.CancellationToken.None);
                        }
                        else
                        {
                            var scpi = GetDefaultScpiForCurrentProfile(step);
                            await _scope.SendRawWriteAsync(scpi, _cts?.Token ?? System.Threading.CancellationToken.None);
                        }
                    }

                    // Now perform the actual dump using the textbox (override) command
                    var cmd = (txtCmdDumpImage.Text ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(cmd))
                        throw new InvalidOperationException("DumpImage command is empty.");

                    var raw = await _scope.SendRawDumpAndReadAsync(cmd, _cts?.Token ?? System.Threading.CancellationToken.None);
                    var payload = StripIeee4882Block(raw);

                    if (payload == null || payload.Length == 0)
                        throw new InvalidOperationException("No image data received.");

                    // Try to preview the image on the Measurements tab (optional)
                    try
                    {
                        using (var ms = new System.IO.MemoryStream(payload))
                        using (var img = System.Drawing.Image.FromStream(ms))
                        {
                            var clone = (System.Drawing.Image)img.Clone();
                            var old = picScreen.Image;
                            picScreen.Image = clone;
                            try { old?.Dispose(); } catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error("Image decode failed: " + InnermostMessage(ex));
                    }

                    lblStatusDumpImage.Text = "OK";
                    lblStatusDumpImage.ForeColor = System.Drawing.Color.Green;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("DumpImage test failed: " + InnermostMessage(ex));
                    lblStatusDumpImage.Text = "Fail (" + InnermostMessage(ex) + ")";
                    lblStatusDumpImage.ForeColor = System.Drawing.Color.Red;
                }
            });
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

                // Build payloads
                var cfgXml = Oscilloscope_Network_Capture.Core.Online.Online.SerializeConfig(_config);
                var debug = Oscilloscope_Network_Capture.Core.Online.Online.ReadDebugLog();
                var version = Oscilloscope_Network_Capture.Core.Online.Online.CurrentVersion;

                Logger.Instance.Info("Sending feedback to developer...");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(35)))
                {
                    var resp = await Oscilloscope_Network_Capture.Core.Online.Online.SendFeedbackAsync(cfgXml, debug, email, version, cts.Token).ConfigureAwait(true);

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

        private async Task RunSuiteAsync(ScopeTestSuite suite, string primaryScpi, Label statusLabel)
        {
            try
            {
                await EnsureConnectedAsync();
                var steps = ScopeTestSuiteRegistry.Resolve(_config, suite);
                string lastQueryResponse = null;
                bool suiteFailed = false;
                string failReason = null;

                // Separator and suite header before executing
                Logger.Instance.Debug("---");
                Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(suite) + ":");

                foreach (var step in steps)
                {
                    // For the first step we use the textbox content (primaryScpi) to honor overrides; subsequent steps use profile commands
                    if (step == steps.First())
                    {
                        if (IsQuery(step))
                            lastQueryResponse = await _scope.SendRawQueryAsync(primaryScpi, _cts?.Token ?? CancellationToken.None);
                        else
                            await _scope.SendRawWriteAsync(primaryScpi, _cts?.Token ?? CancellationToken.None);
                    }
                    else
                    {
                        // Resolve SCPI from profile and execute
                        if (IsQuery(step))
                        {
                            var scpi = GetDefaultScpiForCurrentProfile(step);
                            lastQueryResponse = await _scope.SendRawQueryAsync(scpi, _cts?.Token ?? CancellationToken.None);
                        }
                        else if (step == ScopeCommand.DumpImage)
                        {
                            // Handled elsewhere; ignore in generic test runner
                        }
                        else
                        {
                            var scpi = GetDefaultScpiForCurrentProfile(step);
                            await _scope.SendRawWriteAsync(scpi, _cts?.Token ?? CancellationToken.None);
                        }
                    }

                    // Evaluate system error responses and mark failure if non-zero
                    if (step == ScopeCommand.PopLastSystemError)
                    {
                        if (TryParseSystemErrorCode(lastQueryResponse, out var code))
                        {
                            if (code != 0)
                            {
                                suiteFailed = true;
                                failReason = "SysErr: " + (lastQueryResponse ?? string.Empty);
                                break;
                            }
                        }
                        else
                        {
                            var s = (lastQueryResponse ?? string.Empty).Trim();
                            if (!string.IsNullOrEmpty(s) && s.IndexOf("no error", StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                suiteFailed = true;
                                failReason = "SysErr: " + s;
                                break;
                            }
                        }
                    }
                }

                // Existing: log IDN breakdown for QueryIdentify
                if (!suiteFailed && suite == ScopeTestSuite.QueryIdentify && !string.IsNullOrWhiteSpace(lastQueryResponse))
                {
                    LogIdnVendorModelFirmware(lastQueryResponse);
                }

                // NEW: final Info lines per suite (printed last)
                if (!suiteFailed)
                {
                    switch (suite)
                    {
                        case ScopeTestSuite.ClearStatistics:
                            Logger.Instance.Info("Statistics cleared");
                            break;

                        case ScopeTestSuite.QueryActiveTrigger:
                            {
                                var s = TrimQuotes(lastQueryResponse ?? string.Empty).Trim();
                                Logger.Instance.Info("Trigger status set to " + (string.IsNullOrEmpty(s) ? "(empty)" : s));
                                break;
                            }

                        case ScopeTestSuite.Stop:
                            Logger.Instance.Info("Trigger mode has been set to STOP");
                            break;

                        case ScopeTestSuite.Run:
                            Logger.Instance.Info("Trigger mode has been set to RUN");
                            break;

                        case ScopeTestSuite.Single:
                            Logger.Instance.Info("Trigger mode has been set to SINGLE");
                            break;

                        case ScopeTestSuite.QueryTriggerMode:
                            {
                                var s = TrimQuotes(lastQueryResponse ?? string.Empty).Trim();
                                Logger.Instance.Info("Trigger mode is set to " + (string.IsNullOrEmpty(s) ? "(empty)" : s));
                                break;
                            }
                    }
                }

                if (suiteFailed)
                {
                    statusLabel.Text = "Fail (" + (failReason ?? string.Empty) + ")";
                    statusLabel.ForeColor = Color.Red;
                }
                else
                {
                    statusLabel.Text = "OK";
                    statusLabel.ForeColor = Color.Green;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Suite failed: " + InnermostMessage(ex));
                statusLabel.Text = "Fail (" + InnermostMessage(ex) + ")";
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
                case ScopeCommand.PopLastSystemError:
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
                lbl.Text = string.Format("SCPI commands for {0} {1}", vendor, modelDisplay);
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

        private async Task CheckForNewVersionAsync()
        {
            try { System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12; } catch { }

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
                {
                    var newest = await Oscilloscope_Network_Capture.Core.Online.Online.GetNewestVersionAsync(cts.Token).ConfigureAwait(true);
                    var current = Oscilloscope_Network_Capture.Core.Online.Online.CurrentVersion?.Trim() ?? string.Empty;
                    newest = (newest ?? string.Empty).Trim();

                    if (!string.IsNullOrWhiteSpace(newest) &&
                        !string.Equals(newest, current, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Instance.Debug("---");
                        Logger.Instance.Info("A newer version is available: " + newest);
                        ShowNewVersionBadge(newest);
                    }
                    else
                    {
                        Logger.Instance.Debug("You are on the latest version (" + current + ").");
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
            rtb.AppendText("Capture mode enabled. Press [ENTER] to capture image and [ESC] to exit capture mode. View \"Help\" tab to see all keyboard commands available." + Environment.NewLine);
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

            // Append PREVIOUS filename if available
            if (_savedFileHistory.Count > 0)
            {
                var prev = _savedFileHistory[_savedFileHistory.Count - 1];
                rtb.AppendText(Environment.NewLine + "Previous image was saved with filename [");

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

                rtb.AppendText("]");
            }
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
            if (_btnCaptureStart != null) _btnCaptureStart.Enabled = false;
            this.KeyPreview = true;
            this.KeyDown -= Form1_Capture_KeyDown;
            this.KeyDown += Form1_Capture_KeyDown;
            Logger.Instance.Info("Capture mode enabled.");

            // Update action box with instructions and next filename (bold)
            UpdateActionRichTextForNextFilename();
        }

        private void ExitCaptureMode()
        {
            _captureMode = false;
            if (_btnCaptureStart != null) _btnCaptureStart.Enabled = true;
            this.KeyDown -= Form1_Capture_KeyDown;
            Logger.Instance.Info("Capture mode disabled.");
        }

        // Modify the capture-mode key handler to include DELETE
        private void Form1_Capture_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_captureMode) return;

            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = CaptureAndSaveAsync();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ExitCaptureMode();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                if (checkBoxEnableDelete.Checked)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DeleteLastSavedFile();
                }
            }
            // * => SINGLE (Numpad * or Shift+8)
            else if (e.KeyCode == Keys.Multiply || (e.KeyCode == Keys.D8 && e.Shift))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = RunSuiteHeadlessAsync(ScopeTestSuite.Single);
            }
            // / => RUN (Numpad / or / key)
            else if (e.KeyCode == Keys.Divide || e.KeyCode == Keys.OemQuestion)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = RunSuiteHeadlessAsync(ScopeTestSuite.Run);
            }
            // UP => adjust trigger up
            else if (e.KeyCode == Keys.Up)
            {
                // Treat Oemplus as + (works even without Shift on some layouts)
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = AdjustTriggerLevelAsync(+1);
            }
            // - => adjust trigger down
            else if (e.KeyCode == Keys.Down)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = AdjustTriggerLevelAsync(-1);
            }
            // PLUS => zoom-in (smaller TIME/DIV)
            else if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = AdjustTimeDivAsync(+1);
            }
            // MINUS => zoom-out (larger TIME/DIV)
            else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = AdjustTimeDivAsync(-1);
            }
        }

        // Query current TIME/DIV via suite and parse seconds/div as double
        // Query current TIME/DIV via suite and parse seconds/div as double
        private async Task<double> QueryTimeDivViaSuiteAsync()
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.QueryTimeDiv) + ":");

            var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.QueryTimeDiv);
            string primary = GetPrimaryScpiForSuite(ScopeTestSuite.QueryTimeDiv);
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

            // Parse e.g. "2.000000e-06"
            double seconds;
            if (!double.TryParse((resp ?? string.Empty).Trim(),
                                 System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowLeadingSign,
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out seconds))
            {
                var m = System.Text.RegularExpressions.Regex.Match(resp ?? string.Empty, @"[+-]?(?:\d+\.?\d*|\d*\.?\d+)(?:[eE][+-]?\d+)?");
                if (!m.Success || !double.TryParse(m.Value,
                                                    System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowLeadingSign,
                                                    System.Globalization.CultureInfo.InvariantCulture,
                                                    out seconds))
                    throw new InvalidOperationException("Unable to parse TIME/DIV: " + (resp ?? "(empty)"));
            }

            // NEW: human-readable Info line
            Logger.Instance.Info("TIME/DIV is set to " + FormatSecondsToSi(seconds));

            return seconds;
        }

        // Execute SetTimeDiv suite with a specific target seconds/div
        private async Task RunSetTimeDivSuiteAsync(double targetSeconds)
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.SetTimeDiv) + ":");

            var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.SetTimeDiv);

            // Resolve format: prefer override ONLY if it still has {0}; else use profile default
            string overrideFmt = (txtCmdTimeDivSet?.Text ?? string.Empty).Trim();
            string profileFmt = GetDefaultScpiForCurrentProfile(ScopeCommand.SetTimeDiv) ?? string.Empty;

            string fmtToUse = (!string.IsNullOrWhiteSpace(overrideFmt) && overrideFmt.IndexOf("{0}", StringComparison.Ordinal) >= 0)
                                ? overrideFmt
                                : profileFmt;

            // Prefer exponential seconds representation to avoid locale issues
            string formattedSeconds = targetSeconds.ToString("0.#########e+0", CultureInfo.InvariantCulture);

            string primaryScpi;
            if (!string.IsNullOrWhiteSpace(fmtToUse) && fmtToUse.IndexOf("{0}", StringComparison.Ordinal) >= 0)
            {
                primaryScpi = string.Format(CultureInfo.InvariantCulture, fmtToUse, formattedSeconds);
            }
            else if (!string.IsNullOrWhiteSpace(fmtToUse))
            {
                // Last resort if neither override nor profile contains {0}
                primaryScpi = (fmtToUse + " " + formattedSeconds).Trim();
            }
            else
            {
                // Extremely unlikely: no command available; send raw numeric (driver could reject)
                primaryScpi = formattedSeconds;
            }

            bool first = true;
            foreach (var step in steps)
            {
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
                    continue;

                if (IsQuery(step))
                    await _scope.SendRawQueryAsync(scpi, ct).ConfigureAwait(true);
                else
                    await _scope.SendRawWriteAsync(scpi, ct).ConfigureAwait(true);
            }

            // Log as the last line for this suite
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

        /*
        // Decimal-based grid helpers to avoid floating-point stalls around values like 2.299990
        private static bool IsOnGrid(double value, double step)
        {
            if (step <= 0) return false;

            // Quantize input to 1e-6 to match instrument formatting, then work in decimal
            decimal v = Math.Round((decimal)value, 6, MidpointRounding.AwayFromZero);
            decimal s = (decimal)step;

            if (s == 0) return false;

            decimal q = v / s; // value in "grid units"
            decimal nearest = Math.Round(q, 0, MidpointRounding.AwayFromZero);

            // Tolerance in grid units (e.g., 0.0002 of a step)
            const decimal tolQ = 0.0002m;
            return Math.Abs(q - nearest) <= tolQ;
        }
        */

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

            // Suite header
            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.QueryTriggerLevel) + ":");

            // Resolve steps (usually a single query)
            var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.QueryTriggerLevel);
            string primary = GetPrimaryScpiForSuite(ScopeTestSuite.QueryTriggerLevel);
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

            // Parse a double like "3.299990e+00"
            double volts;
            if (!double.TryParse((resp ?? string.Empty).Trim(), NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out volts))
            {
                // Fallback: extract first floating token
                var m = Regex.Match(resp ?? string.Empty, @"[+-]?(?:\d+\.?\d*|\d*\.?\d+)(?:[eE][+-]?\d+)?");
                if (!m.Success || !double.TryParse(m.Value, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out volts))
                    throw new InvalidOperationException("Unable to parse trigger level: " + (resp ?? "(empty)"));
            }

            // Final, human-readable Info line (printed last for this suite)
            Logger.Instance.Info("Trigger level is set to " + FormatVoltsToSi(volts));

            return volts;
        }

        // Executes the SetTriggerLevel suite where the first command is formatted with targetVolts
        private async Task RunSetTriggerLevelSuiteAsync(double targetVolts)
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            // Suite header
            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(ScopeTestSuite.SetTriggerLevel) + ":");

            var steps = ScopeTestSuiteRegistry.Resolve(_config, ScopeTestSuite.SetTriggerLevel);

            // Prefer textbox override if present; else use profile default
            string fmt = (txtCmdTrigLevelSet?.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fmt))
                fmt = GetDefaultScpiForCurrentProfile(ScopeCommand.SetTriggerLevel) ?? string.Empty;

            // Build primary SCPI robustly (supports both "{0}" and literal commands)
            string primaryScpi = InjectNumericArg(fmt, targetVolts);
            if (string.IsNullOrWhiteSpace(primaryScpi))
            {
                var profFmt = GetDefaultScpiForCurrentProfile(ScopeCommand.SetTriggerLevel) ?? ":TRIGGER:EDGE:LEVEL {0}";
                primaryScpi = InjectNumericArg(profFmt, targetVolts);
            }

            bool first = true;
            foreach (var step in steps)
            {
                if (first)
                {
                    // First step should be SetTriggerLevel
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
                    continue;

                if (IsQuery(step))
                    await _scope.SendRawQueryAsync(scpi, ct).ConfigureAwait(true);
                else
                    await _scope.SendRawWriteAsync(scpi, ct).ConfigureAwait(true);
            }

            // Final, human-readable Info line (printed last for this suite)
            Logger.Instance.Info("Trigger level is set to " + FormatVoltsToSi(targetVolts));
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
                case ScopeTestSuite.QueryActiveTrigger: return txtCmdActiveTrig?.Text ?? string.Empty;
                case ScopeTestSuite.Stop: return txtCmdStop?.Text ?? string.Empty;
                case ScopeTestSuite.Run: return txtCmdRun?.Text ?? string.Empty;
                case ScopeTestSuite.Single: return txtCmdSingle?.Text ?? string.Empty;
                case ScopeTestSuite.QueryTriggerMode: return txtCmdTrigMode?.Text ?? string.Empty;
                case ScopeTestSuite.QueryTriggerLevel: return txtCmdTrigLevelQ?.Text ?? string.Empty;
                case ScopeTestSuite.SetTriggerLevel: return txtCmdTrigLevelSet?.Text ?? string.Empty;
                case ScopeTestSuite.QueryTimeDiv: return txtCmdTimeDivQ?.Text ?? string.Empty;
                case ScopeTestSuite.SetTimeDiv: return txtCmdTimeDivSet?.Text ?? string.Empty;
                case ScopeTestSuite.DumpImage: return txtCmdDumpImage?.Text ?? string.Empty;
                case ScopeTestSuite.PopLastSystemError: return txtCmdSysErr?.Text ?? string.Empty;
                case ScopeTestSuite.OperationComplete: return txtCmdOpc?.Text ?? string.Empty;
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

            // Separator and suite header before executing
            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(suite) + ":");

            foreach (var step in steps)
            {
                if (step == steps.First())
                {
                    if (IsQuery(step))
                        lastQueryResponse = await _scope.SendRawQueryAsync(primary, _cts?.Token ?? CancellationToken.None);
                    else if (step == ScopeCommand.DumpImage)
                    {
                        // DumpImage is handled in CaptureAndSaveAsync; skip here
                    }
                    else
                        await _scope.SendRawWriteAsync(primary, _cts?.Token ?? CancellationToken.None);
                }
                else
                {
                    if (step == ScopeCommand.DumpImage)
                    {
                        // DumpImage is handled in CaptureAndSaveAsync; skip here
                        continue;
                    }
                    if (IsQuery(step))
                    {
                        var scpi = GetDefaultScpiForCurrentProfile(step);
                        lastQueryResponse = await _scope.SendRawQueryAsync(scpi, _cts?.Token ?? CancellationToken.None);
                    }
                    else
                    {
                        var scpi = GetDefaultScpiForCurrentProfile(step);
                        await _scope.SendRawWriteAsync(scpi, _cts?.Token ?? CancellationToken.None);
                    }
                }
            }

            // Existing: log IDN breakdown for QueryIdentify
            if (suite == ScopeTestSuite.QueryIdentify && !string.IsNullOrWhiteSpace(lastQueryResponse))
            {
                LogIdnVendorModelFirmware(lastQueryResponse);
            }

            // NEW: final Info lines per suite (printed last)
            switch (suite)
            {
                case ScopeTestSuite.ClearStatistics:
                    Logger.Instance.Info("Statistics cleared");
                    break;

                case ScopeTestSuite.QueryActiveTrigger:
                    {
                        var s = TrimQuotes(lastQueryResponse ?? string.Empty).Trim();
                        Logger.Instance.Info("Trigger status set to " + (string.IsNullOrEmpty(s) ? "(empty)" : s));
                        break;
                    }

                case ScopeTestSuite.Stop:
                    Logger.Instance.Info("Trigger mode has been set to STOP");
                    break;

                case ScopeTestSuite.Run:
                    Logger.Instance.Info("Trigger mode has been set to RUN");
                    break;

                case ScopeTestSuite.Single:
                    Logger.Instance.Info("Trigger mode has been set to SINGLE");
                    break;

                case ScopeTestSuite.QueryTriggerMode:
                    {
                        var s = TrimQuotes(lastQueryResponse ?? string.Empty).Trim();
                        Logger.Instance.Info("Trigger mode is set to " + (string.IsNullOrEmpty(s) ? "(empty)" : s));
                        break;
                    }
            }
        }

        private async Task CaptureAndSaveAsync()
        {
            bool savedOk = false;

            try
            {
                // Visual cue: clear current image and set black background
                try
                {
                    var old = picScreen.Image;
                    picScreen.Image = null;
                    try { old?.Dispose(); } catch { }
                    picScreen.BackColor = Color.Black;
                }
                catch { }

                // Optional start beep
                if (_config != null && _config.EnableBeep) BeepStart();

                await EnsureConnectedAsync();

                // Optional pre-clear step
                if (_config != null && _config.ForceClear)
                {
                    try
                    {
                        await RunSuiteHeadlessAsync(ScopeTestSuite.ClearStatistics);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error("ClearStatistics suite failed before capture: " + InnermostMessage(ex));
                    }

                    // Optional delay after clear
                    try
                    {
                        var delayMs = Math.Max(0, _config?.DelayMs ?? 0);
                        if (delayMs > 0)
                        {
                            Logger.Instance.Debug("---");
                            Logger.Instance.Debug("Delay before capture: " + delayMs.ToString(CultureInfo.InvariantCulture) + " ms");
                            await Task.Delay(delayMs, _cts?.Token ?? System.Threading.CancellationToken.None);
                            Logger.Instance.Debug("Delay completed.");
                        }
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error("Delay after ClearStatistics failed: " + InnermostMessage(ex));
                    }
                }

                // NEW: Stop acquisition before DumpImage
                try
                {
                    await RunSuiteHeadlessAsync(ScopeTestSuite.Stop);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Stop suite before capture failed: " + InnermostMessage(ex));
                }

                // Perform capture via the DumpImage suite (executed and logged inside CaptureScreenAsync)
                var data = await _scope.CaptureScreenAsync(_cts?.Token ?? System.Threading.CancellationToken.None);
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

                    // Overwrite if it exists
                    try { if (File.Exists(path)) File.Delete(path); } catch { }

                    // Save PNG
                    img.Save(path, System.Drawing.Imaging.ImageFormat.Png);

                    // Show in UI (clone to avoid using disposed image)
                    try
                    {
                        var clone = (Image)img.Clone();
                        var old = picScreen.Image;
                        picScreen.Image = clone;
                        try { old?.Dispose(); } catch { }
                    }
                    catch { }

                    Logger.Instance.Info("Saved capture: " + path);
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

                // Auto-increment NUMBER after a successful save in capture mode
                if (savedOk && _captureMode)
                {
                    IncrementNumberAfterSuccessfulSave();
                    UpdateActionRichTextForNextFilename(); // Refresh to show updated next + previous
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Capture failed: " + InnermostMessage(ex));
            }
            finally
            {
                // Optional end beep (lower tone)
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
            Logger.Instance.Debug("   hest 1 Auto-connected.");
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

        private static string TrimQuotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
                return s.Substring(1, s.Length - 2);
            return s;
        }

        private static (string Vendor, string Model, string Firmware) ParseIdn(string idn)
        {
            if (string.IsNullOrWhiteSpace(idn)) return ("", "", "");
            var parts = idn.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => TrimQuotes(p).Trim()).ToArray();
            string vendor = parts.Length > 0 ? parts[0] : "";
            string model = parts.Length > 1 ? parts[1] : "";
            string firmware = parts.Length > 3 ? parts[3] : (parts.Length > 2 ? parts[2] : "");
            return (vendor, model, firmware);
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
            if (tabConfigMisc == null) return;
            EnsureVariableNamesListSize();

            // Remove existing dynamic controls
            var toRemove = tabConfigMisc.Controls.Cast<Control>()
                .Where(c => (c.Name ?? string.Empty).StartsWith(VarNameEditorPrefix) || (c.Name ?? string.Empty).StartsWith(VarNameLabelPrefix))
                .ToList();
            foreach (var c in toRemove) { try { tabConfigMisc.Controls.Remove(c); c.Dispose(); } catch { } }

            // Position just below numericUpDownVariables with tighter spacing
            int baseY = (numericUpDownVariables != null) ? numericUpDownVariables.Bottom + 6 : 320;
            int xText = 13;
            int rowH = 26;

            int nameTbWidth = numericUpDownVariables != null ? numericUpDownVariables.Width : 220;
            int xLabel = xText + nameTbWidth + 6; // label closer to textbox

            // Dynamic variables (editable)
            for (int i = 0; i < (_config?.VariableCount ?? 0); i++)
            {
                // Textbox first: variable name editor (uppercase)
                var tb = new TextBox
                {
                    Name = VarNameEditorPrefix + i,
                    Location = new Point(xText, baseY + i * rowH),
                    Width = nameTbWidth,
                    Text = (_config.VariableNames.ElementAtOrDefault(i) ?? ("VAR" + (i + 1)))
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
                    var nameLblRef = tabConfigMisc.Controls.Find(VarNameLabelPrefix + idx, true).FirstOrDefault() as Label;
                    if (nameLblRef != null)
                    {
                        var nm = string.IsNullOrWhiteSpace(upper) ? ("VAR" + (idx + 1)) : upper;
                        nameLblRef.Text = "{" + nm + "}";
                        try { nameLblRef.Font = label2?.Font ?? nameLblRef.Font; } catch { }
                    }
                    UpdateActionRichTextForNextFilename();
                };
                tabConfigMisc.Controls.Add(tb);

                // Label to the right: shows current variable name token {NAME}
                var nameLbl = new Label
                {
                    Name = VarNameLabelPrefix + i,
                    AutoSize = true,
                    Location = new Point(xLabel, baseY + i * rowH + 3),
                };
                var nmTxt = _config.VariableNames.ElementAtOrDefault(i);
                if (string.IsNullOrWhiteSpace(nmTxt)) nmTxt = "VAR" + (i + 1);
                nameLbl.Text = "{" + nmTxt.ToUpperInvariant() + "}";
                try { nameLbl.Font = label2?.Font ?? nameLbl.Font; } catch { }
                tabConfigMisc.Controls.Add(nameLbl);
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
            tabConfigMisc.Controls.Add(tbDate);

            var lblDate = new Label
            {
                Name = VarNameLabelPrefix + "DATE",
                AutoSize = true,
                Location = new Point(xLabel, afterDynamicY + 3),
                Text = "{DATE}"
            };
            try { lblDate.Font = label2?.Font ?? lblDate.Font; } catch { }
            tabConfigMisc.Controls.Add(lblDate);

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
            tabConfigMisc.Controls.Add(tbTime);

            var lblTime = new Label
            {
                Name = VarNameLabelPrefix + "TIME",
                AutoSize = true,
                Location = new Point(xLabel, afterDynamicY + rowH + 3),
                Text = "{TIME}"
            };
            try { lblTime.Font = label2?.Font ?? lblTime.Font; } catch { }
            tabConfigMisc.Controls.Add(lblTime);

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

            // Dynamic (editable) variables
            for (int i = 0; i < desired; i++)
            {
                // Textbox first: variable value editor
                var tb = new TextBox
                {
                    Name = VarTextPrefix + i,
                    Location = new Point(xText, baseY + i * rowH),
                    Width = textWidth,
                    Text = _config.VariableValues.ElementAtOrDefault(i) ?? string.Empty
                };
                int idx = i;
                tb.TextChanged += (s, a) =>
                {
                    EnsureVariableNamesListSize();
                    _config.VariableValues[idx] = tb.Text ?? string.Empty;
                    ConfigurationService.Save(_config);
                    UpdateActionRichTextForNextFilename();
                };
                tabCapturing.Controls.Add(tb);

                // Label: token name {NAME}
                var name = _config.VariableNames.ElementAtOrDefault(i);
                if (string.IsNullOrWhiteSpace(name)) name = "VAR" + (i + 1);
                var lbl = new Label
                {
                    Name = VarLabelPrefix + i,
                    AutoSize = true,
                    Location = new Point(xLabel, baseY + i * rowH + 3),
                    Text = "{" + name.ToUpperInvariant() + "}"
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
                Text = "{DATE}"
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
                Text = "{TIME}"
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
                    buttonCaptureStart.Location = new Point(buttonCaptureStart.Location.X, newY);
                }
            }
            catch { }
        }

        /*
        private int GetCurrentVariableCount() => _config?.VariableCount ?? 0;

        private void VarValue_TextChanged(object sender, EventArgs e)
        {
            // Not used in dynamic layout.
        }
        */

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                rtbLog.Clear();
                Logger.Instance.ClearLog();
                Logger.Instance.Info("Log cleared by user.");
            }
            catch { }
        }

        // MODIFIED: use QueryIdentify suite and suppress the "ID: ..." Info line
        private async Task ConnectAndRefreshAsync()
        {
            try
            {
                lblStatus.Text = "Checking...";
                lblStatus.ForeColor = Color.Gray;

                var vendor = cboVendor.SelectedItem as string;
                var modelDisplay = cboModel.SelectedItem as string;
                var modelPattern = ToModelPattern(modelDisplay);

                // Persist current selection/connection settings
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
                    return;
                }

                if (_scope == null || !_scope.IsConnected)
                {
                    try { if (_scope != null) await _scope.DisconnectAsync(); } catch { }
                    _scope = ScopeFactory.Create(vendor, modelPattern, resource);

                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();

                    Logger.Instance.Info(string.Format("Connecting to {0} {1} at {2}.", vendor, modelDisplay, resource));
                    await _scope.ConnectAsync(_cts.Token);
                    Logger.Instance.Info("Network session established.");
                }
                else
                {
                    Logger.Instance.Info("Network session already established.");
                }

                // Change 1: run the QueryIdentify suite instead of IdentifyAsync
                var idn = await QueryIdentifyViaSuiteAsync().ConfigureAwait(true);

                // Change 2: suppress the Info line that logs "ID: ..."
                // Logger.Instance.Info(string.Format("ID: {0}", idn)); // removed

                var parsed = ParseIdn(idn);
                Logger.Instance.Info("Oscilloscope is identified as:");
                Logger.Instance.Info("Vendor: " + parsed.Vendor);
                Logger.Instance.Info("Model: " + parsed.Model);
                Logger.Instance.Info("Firmware: " + parsed.Firmware);

                lblStatus.Text = "Success - oscilloscope is connectable via network";
                lblStatus.ForeColor = Color.Green;

                // Refresh commands and re-apply any overrides
                PopulateCommandTextboxes();
                ApplyScpiOverridesForCurrentProfile();
            }
            catch (TimeoutException tex)
            {
                Logger.Instance.Error("Network session cannot be established: " + InnermostMessage(tex));
                lblStatus.Text = "Network session cannot be established (timeout)";
                lblStatus.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Connectivity check failed: " + InnermostMessage(ex));
                lblStatus.Text = "Failure - oscilloscope is not connectable via network";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void LogIdnVendorModelFirmware(string idn)
        {
            try
            {
                var parsed = ParseIdn(idn);
                if (!string.IsNullOrWhiteSpace(parsed.Vendor)) Logger.Instance.Info("Vendor: " + parsed.Vendor);
                if (!string.IsNullOrWhiteSpace(parsed.Model)) Logger.Instance.Info("Model: " + parsed.Model);
                if (!string.IsNullOrWhiteSpace(parsed.Firmware)) Logger.Instance.Info("Firmware: " + parsed.Firmware);
            }
            catch { /* ignore parse/log failures */ }
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
    }
}
