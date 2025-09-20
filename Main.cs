using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Oscilloscope_Network_Capture
{
    public partial class Main : Form
    {
        #region Constants and Fields

        private string scopeIp = "192.168.0.100";
        private int scopePort = 5555;
        private string component = "U1";
        private string outputFolder = "output";
        private string filenameFormat = "{Component}_{Number}_{Region}_{Date}_{Time}";
        private string oncPage = "https://commodore-repair-toolbox.dk";
        private string oncPageAutoUpdate = "/auto-update/";
        private bool beepEnabled = true;

        private readonly Logger _logger = new Logger();
        private Configuration _config;
        private Scope _scope;

        private string versionThis = "";
        private readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Oscilloscope Network Capture.cfg");

        private Color pictureBoxBorderColor = Color.Red;
        private readonly int pictureBoxBorderThickness = 2;
        private bool initializing = false;

        private bool forceAcquisition = false;

        // Hotkey mode
        private bool hotkeyMode = false;
        private bool captureInProgress = false;
        private bool failMode = false;

        // Number range
        private int numberStart = 1;
        private int originalNumberStart;
        private bool numberRangeActive = false;

        private bool _suppressSplitterSave = true;
        private bool windowMaximized = false;
        private FormWindowState _lastNonMinimizedState = FormWindowState.Normal;

        #endregion

        #region Constructor and Form lifecycle

        // ################################################################################################
        // Initializes the form, wires events, sets up logging, loads configuration, and prepares UI state.
        // ################################################################################################
        public Main()
        {
            initializing = true;
            InitializeComponent();

            // Route logger messages to the UI (with debug filtering).
            _logger.Message += entry =>
            {
                if (richTextBoxLog == null) return;

                if (entry.Level == LogLevel.Debug && (checkBoxDebug == null || !checkBoxDebug.Checked))
                    return;

                if (richTextBoxLog.InvokeRequired)
                    richTextBoxLog.BeginInvoke(new Action(() => AppendColoredLine(entry.Message, entry.Level)));
                else
                    AppendColoredLine(entry.Message, entry.Level);
            };

            // Initialize services.
            _config = new Configuration(configPath, _logger);
            _scope = new Scope(_logger);

            EnsureLog();
            InitializeVersionLabel();

            if (textBoxIp != null) textBoxIp.Text = scopeIp;

            if (comboBoxRegion != null)
            {
                comboBoxRegion.SelectedIndexChanged += comboBoxRegion_SelectedIndexChanged;
                if (comboBoxRegion.Items.Count > 0)
                    comboBoxRegion.SelectedIndex = 0;
            }

            checkBoxBeep.CheckedChanged += checkBoxBeep_CheckedChanged;

            LoadConfig();
            LoadHelpText();
            LoadAboutText();

            // Resolve default folder name -> absolute path if still the literal "output"
            EnsureDefaultOutputFolderResolved();

            // Mirror state to UI controls
            textBoxCaptureFolder.Text = outputFolder;
            textBoxCaptureNumberStart.KeyUp += textBoxCaptureNumberStart_KeyUp;
            textBoxCaptureNumberStart.Enter += textBoxCaptureNumberStart_Enter;

            textBoxComponent.Text = component;
            textBoxComponent.Leave += textBoxComponent_Leave;
            textBoxComponent.KeyDown += textBoxComponent_KeyDown;
            textBoxComponent.TextChanged += textBoxComponent_TextChanged;
            textBoxFilenameFormat.Text = filenameFormat;

            textBoxPort.Text = scopePort.ToString();
            textBoxPort.Leave += textBoxPort_Leave;
            textBoxPort.KeyDown += textBoxPort_KeyDown;
            textBoxFilenameFormat.Leave += textBoxFilenameFormat_Leave;
            textBoxFilenameFormat.KeyDown += textBoxFilenameFormat_KeyDown;
            buttonCaptureContinuelsy.Click += buttonCaptureContinuelsy_Click;
            buttonCheckScope.Click += buttonCheckScope_Click;
            richTextBoxAbout.LinkClicked += richTextBoxAbout_LinkClicked;

            // Keyboard event routing (form-level hotkeys)
            this.KeyPreview = true;
            this.KeyDown += Form_KeyDown;

            // Picture box visuals
            pictureBoxImage.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxImage.BorderStyle = BorderStyle.None;
            pictureBoxImage.Paint += PictureBoxImage_Paint;
            pictureBoxIcon.SizeMode = PictureBoxSizeMode.Zoom;

            // IP input handlers
            textBoxIp.Leave += textBoxIp_Leave;
            textBoxIp.KeyDown += textBoxIp_KeyDown;

            // Other UI elements
            checkBoxForceAcquisition.CheckedChanged += checkBoxForceAcquisition_CheckedChanged;
            checkBoxForceAcquisition.Checked = forceAcquisition;
            splitContainer1.SplitterMoved += splitContainer1_SplitterMoved;
            checkBoxBeep.Checked = beepEnabled;

            // Place "new version" label
            labelNewVersionAvailable.Location = new Point(richTextBoxLog.Right - labelNewVersionAvailable.Width, richTextBoxLog.Top);
            labelNewVersionAvailable.BringToFront();

            // Initial action text
//            richTextBoxAction.Text = "Ready for capture";
            initializing = false;

            // Post-shown checks
            this.Shown += Form1_Shown;
        }

        // ################################################################################################
        // On form shown: finishes UI initialization, checks connectivity, and schedules an online version check.
        // ################################################################################################
        private async void Form1_Shown(object sender, EventArgs e)
        {
            _suppressSplitterSave = false; // allow future splitter saves
            buttonCaptureContinuelsy.Focus();

            // Connectivity check identical to the button action
            var ok = await CheckScopeConnectivityAsync();
            if (richTextBoxAction != null)
                richTextBoxAction.Text = ok ? "Ready for capture" : "Cannot connect to scope";

            try
            {
                await Task.Delay(5000).ConfigureAwait(true);
                await CheckOnlineVersionAsync();
            }
            catch
            {
                // Avoid startup crash on any unexpected network error
            }
        }

        // ################################################################################################
        // Persists window maximize/restore state on resize (ignores minimized).
        // ################################################################################################
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.WindowState == FormWindowState.Minimized) return;

            _lastNonMinimizedState = this.WindowState;
            bool isMax = _lastNonMinimizedState == FormWindowState.Maximized;

            if (isMax != windowMaximized)
            {
                windowMaximized = isMax;
                Log("Window " + (isMax ? "maximized" : "restored") + ".", LogLevel.Debug);
                if (!initializing) SaveConfig();
            }
        }

        // ################################################################################################
        // Persists layout/state on close, including last non-minimized window state.
        // ################################################################################################
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            windowMaximized = (_lastNonMinimizedState == FormWindowState.Maximized);
            try { SaveConfig(); } catch { }
            base.OnFormClosing(e);
        }

        #endregion

        #region Configuration and Layout

        // ################################################################################################
        // Loads persisted configuration from disk and applies it to runtime state and UI controls.
        // ################################################################################################
        private void LoadConfig()
        {
            _config.Load();

            scopeIp = _config.ScopeIp;
            if (textBoxIp != null) textBoxIp.Text = scopeIp;

            scopePort = _config.ScopePort;
            if (textBoxPort != null) textBoxPort.Text = scopePort.ToString();

            component = _config.Component ?? "U1";
            if (textBoxComponent != null) textBoxComponent.Text = component;

            filenameFormat = _config.FilenameFormat ?? filenameFormat;
            if (textBoxFilenameFormat != null) textBoxFilenameFormat.Text = filenameFormat;

            beepEnabled = _config.BeepEnabled;
            if (checkBoxBeep != null) checkBoxBeep.Checked = beepEnabled;

            forceAcquisition = _config.ForceAcquisition;
            if (checkBoxForceAcquisition != null) checkBoxForceAcquisition.Checked = forceAcquisition;

            // Region
            var region = _config.Region ?? "";
            if (comboBoxRegion != null && !string.IsNullOrWhiteSpace(region))
            {
                for (int i = 0; i < comboBoxRegion.Items.Count; i++)
                {
                    var item = comboBoxRegion.Items[i] as string;
                    if (string.Equals(item, region, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBoxRegion.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Output folder
            outputFolder = _config.NormalizeOutputFolder(_config.OutputFolder);
            if (textBoxCaptureFolder != null) textBoxCaptureFolder.Text = outputFolder;

            // Splitter distance
            if (_config.SplitterDistance.HasValue)
                ApplySplitterDistance(_config.SplitterDistance.Value);

            // Window state
            windowMaximized = _config.WindowMaximized;
            this.WindowState = windowMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
            _lastNonMinimizedState = this.WindowState;
        }

        // ################################################################################################
        // Saves current runtime and UI state back to the configuration file.
        // ################################################################################################
        private void SaveConfig()
        {
            _config.ScopeIp = scopeIp;
            _config.ScopePort = scopePort;
            _config.Component = component;
            _config.FilenameFormat = filenameFormat;
            _config.BeepEnabled = beepEnabled;
            _config.ForceAcquisition = forceAcquisition;
            _config.Region = comboBoxRegion != null ? (comboBoxRegion.SelectedItem as string ?? "").Trim() : "";
            _config.OutputFolder = outputFolder;
            _config.SplitterDistance = (splitContainer1 != null ? (int?)splitContainer1.SplitterDistance : null);
            _config.WindowMaximized = windowMaximized;

            _config.Save();
        }

        // ################################################################################################
        // Applies a splitter distance value while respecting the container panel min sizes.
        // ################################################################################################
        // <param name="distance">Desired splitter distance in pixels.</param>
        private void ApplySplitterDistance(int distance)
        {
            if (splitContainer1 == null) return;
            try
            {
                int min = splitContainer1.Panel1MinSize;
                int max;
                if (splitContainer1.Orientation == Orientation.Vertical)
                    max = splitContainer1.Width - splitContainer1.Panel2MinSize - 1;
                else
                    max = splitContainer1.Height - splitContainer1.Panel2MinSize - 1;

                if (max < min) max = min;
                if (distance < min) distance = min;
                if (distance > max) distance = max;

                splitContainer1.SplitterDistance = distance;
            }
            catch { /* ignore layout timing issues */ }
        }

        // ################################################################################################
        // Handles splitter moved events and persists the new distance unless suppressed during init.
        // ################################################################################################
        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            if (initializing || _suppressSplitterSave) return;
            SaveConfig();
        }

        // ################################################################################################
        // Configures the logger visual style in the rich text box.
        // ################################################################################################
        private void EnsureLog()
        {
            if (richTextBoxLog == null) return;
            richTextBoxLog.ReadOnly = true;
            richTextBoxLog.DetectUrls = false;
            richTextBoxLog.Font = new Font("Consolas", 9f);
            richTextBoxLog.BackColor = Color.Black;
            richTextBoxLog.ForeColor = Color.LightGreen;
            richTextBoxLog.HideSelection = false;
            richTextBoxLog.BorderStyle = BorderStyle.FixedSingle;
        }

        // ################################################################################################
        // Resolves a default or relative output folder to an absolute path and applies it to the UI and config.
        // ################################################################################################
        private void EnsureDefaultOutputFolderResolved()
        {
            var before = outputFolder;
            _config.OutputFolder = string.IsNullOrWhiteSpace(outputFolder) ? "output" : outputFolder;
            _config.EnsureDefaultOutputFolderResolved();
            outputFolder = _config.OutputFolder;

            if (textBoxCaptureFolder != null)
                textBoxCaptureFolder.Text = outputFolder;

            if (!string.Equals(before, outputFolder, StringComparison.OrdinalIgnoreCase))
                SaveConfig();
        }

        #endregion

        #region Output folder management

        // ################################################################################################
        // Opens a folder selection dialog for choosing where captures should be saved. Persists selection and creates directory as needed.
        // ################################################################################################
        private void textBoxCaptureFolder_Click(object sender, EventArgs e)
        {
            try
            {
                outputFolder = NormalizeOutputFolder(outputFolder);
                string initial = outputFolder;

                using (var dlg = new VistaFolderBrowserDialog
                {
                    Description = "Select folder for saving captured oscilloscope images",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true,
                    SelectedPath = Directory.Exists(initial) ? initial : ""
                })
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK &&
                        !string.IsNullOrWhiteSpace(dlg.SelectedPath))
                    {
                        SetOutputFolder(dlg.SelectedPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Folder picker error: " + ex.Message, LogLevel.Error);
            }
        }

        // ################################################################################################
        // Sets the output folder path, normalizes it to absolute, persists to config, and creates it if missing.
        // ################################################################################################
        private void SetOutputFolder(string pathCandidate)
        {
            try
            {
                string abs = NormalizeOutputFolder(pathCandidate);
                if (!string.Equals(abs, outputFolder, StringComparison.OrdinalIgnoreCase))
                {
                    outputFolder = abs;
                    Log("Output folder set to: " + outputFolder, LogLevel.Info);
                    SaveConfig();
                }

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                    Log("Created output folder: " + outputFolder, LogLevel.Info);
                }

                if (textBoxCaptureFolder != null)
                    textBoxCaptureFolder.Text = outputFolder;
            }
            catch (Exception ex)
            {
                Log("Failed to set output folder: " + ex.Message, LogLevel.Error);
                if (textBoxCaptureFolder != null)
                    textBoxCaptureFolder.Text = outputFolder;
            }
        }

        // ################################################################################################
        // Normalizes a raw folder string into an absolute, stable path using the configuration service.
        // ################################################################################################
        private string NormalizeOutputFolder(string raw) => _config.NormalizeOutputFolder(raw);

        // ################################################################################################
        // Opens the current output folder in Windows Explorer, creating it if it doesn't exist.
        // ################################################################################################
        private void buttonOpenFolder_Click(object sender, EventArgs e)
        {
            string outDir = NormalizeOutputFolder(outputFolder);
            if (!Directory.Exists(outDir))
            {
                try { Directory.CreateDirectory(outDir); }
                catch (Exception ex) { Log("Could not create output folder: " + ex.Message, LogLevel.Error); return; }
            }
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + outDir + "\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log("Failed to open output folder: " + ex.Message, LogLevel.Error);
                Beep("error");
            }
        }

        #endregion

        #region Input handlers (textboxes/comboboxes/checkboxes)

        // ################################################################################################
        // Puts the caret at the end of the "start number" textbox when it gains focus (no selection).
        // ################################################################################################
        private void textBoxCaptureNumberStart_Enter(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            tb.SelectionStart = tb.TextLength;
            tb.SelectionLength = 0;
        }

        // ################################################################################################
        // Updates live number status while typing during capture mode; ignores non-content keys.
        // ################################################################################################
        private void textBoxCaptureNumberStart_KeyUp(object sender, KeyEventArgs e)
        {
            if (!hotkeyMode || !numberRangeActive) return;

            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape ||
                e.KeyCode == Keys.Left || e.KeyCode == Keys.Right ||
                e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey ||
                e.KeyCode == Keys.Menu)
                return;

            RefreshNumberRangeFromTextBoxes(logOnError: false);
            UpdateNumberStatusText();
        }

        // ################################################################################################
        // Applies the component textbox content to state, defaulting to "U1" if empty.
        // ################################################################################################
        private void textBoxComponent_Leave(object sender, EventArgs e) => ApplyComponentFromTextBox();

        // ################################################################################################
        // Applies the component textbox when user presses Enter; suppresses ding.
        // ################################################################################################
        private void textBoxComponent_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyComponentFromTextBox();
                e.SuppressKeyPress = true;
            }
        }

        private void textBoxComponent_TextChanged(object sender, EventArgs e)
        {
            if (initializing) return;

            // Update in-memory value, but don't persist on every keystroke
            component = textBoxComponent.Text.Trim();

            // Mirror the live value in the action panel during capture mode
            if (hotkeyMode && numberRangeActive)
                UpdateNumberStatusText();
        }

        // ################################################################################################
        // Validates and applies the component value from the textbox; persists on change.
        // ################################################################################################
        private void ApplyComponentFromTextBox()
        {
            if (textBoxComponent == null) return;
            string raw = textBoxComponent.Text.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                raw = "U1";
                textBoxComponent.Text = raw;
            }
            if (raw != component)
            {
                component = raw;
                if (!initializing) SaveConfig();
            }

            // Keep the action panel in sync when in capture mode
            if (hotkeyMode && numberRangeActive)
                UpdateNumberStatusText();
        }

        // ################################################################################################
        // Applies the filename format on losing focus; validates non-empty.
        // ################################################################################################
        private void textBoxFilenameFormat_Leave(object sender, EventArgs e) => ApplyFilenameFormatFromTextBox();

        // ################################################################################################
        // Applies the filename format when user presses Enter; suppresses ding.
        // ################################################################################################
        private void textBoxFilenameFormat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyFilenameFormatFromTextBox();
                e.SuppressKeyPress = true;
            }
        }

        // ################################################################################################
        // Validates and applies the filename format; reverts to a default if empty.
        // ################################################################################################
        private void ApplyFilenameFormatFromTextBox()
        {
            if (textBoxFilenameFormat == null) return;
            string raw = textBoxFilenameFormat.Text;
            if (string.IsNullOrWhiteSpace(raw))
            {
                Log("Filename format empty; reverting to default.", LogLevel.Warning);
                filenameFormat = "{Component}_{Number}_{Region}";
                textBoxFilenameFormat.Text = filenameFormat;
                SaveConfig();
                return;
            }
            if (raw != filenameFormat)
            {
                filenameFormat = raw;
                Log("Filename format updated: " + filenameFormat, LogLevel.Info);
                SaveConfig();
            }
        }

        // ################################################################################################
        // Applies the port textbox on losing focus; ensures range 1..65535.
        // ################################################################################################
        private void textBoxPort_Leave(object sender, EventArgs e) => ApplyPortFromTextBox();

        // ################################################################################################
        // Applies the port textbox on Enter; suppresses ding.
        // ################################################################################################
        private void textBoxPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyPortFromTextBox();
                e.SuppressKeyPress = true;
            }
        }

        // ################################################################################################
        // Validates and applies the TCP port; logs and reverts on invalid input.
        // ################################################################################################
        private void ApplyPortFromTextBox()
        {
            if (textBoxPort == null) return;
            string raw = textBoxPort.Text.Trim();
            int newPort;
            if (!int.TryParse(raw, out newPort) || newPort < 1 || newPort > 65535)
            {
                Log("Invalid port (must be 1-65535): " + raw, LogLevel.Error);
                textBoxPort.Text = scopePort.ToString();
                return;
            }
            if (newPort != scopePort)
            {
                scopePort = newPort;
                Log("Scope port changed to " + scopePort, LogLevel.Info);
                SaveConfig();
            }
        }

        // ################################################################################################
        // Updates config when region selection changes.
        // ################################################################################################
        private void comboBoxRegion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            SaveConfig();
        }

        // ################################################################################################
        // Applies the IP textbox on losing focus.
        // ################################################################################################
        private void textBoxIp_Leave(object sender, EventArgs e) => ApplyIpFromTextBox();

        // ################################################################################################
        // Applies the IP textbox on Enter; suppresses ding.
        // ################################################################################################
        private void textBoxIp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyIpFromTextBox();
                e.SuppressKeyPress = true;
            }
        }

        // ################################################################################################
        // Validates and applies an IPv4/IPv6 address to state; reverts and logs on invalid input.
        // ################################################################################################
        private void ApplyIpFromTextBox()
        {
            if (textBoxIp == null) return;
            string candidate = textBoxIp.Text.Trim();
            if (ValidateIp(candidate))
            {
                if (candidate != scopeIp)
                {
                    scopeIp = candidate;
                    SaveConfig();
                }
            }
            else
            {
                Log("Invalid IP format: " + candidate, LogLevel.Error);
                textBoxIp.Text = scopeIp;
            }
        }

        // ################################################################################################
        // Toggles the post-capture "force acquisition" behavior and persists it.
        // ################################################################################################
        private void checkBoxForceAcquisition_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxForceAcquisition == null) return;
            forceAcquisition = checkBoxForceAcquisition.Checked;
            Log("Force acquisition after capture: " + (forceAcquisition ? "ON" : "OFF"), LogLevel.Info);
            if (!initializing) SaveConfig();
        }

        // ################################################################################################
        // Toggles audible beeps and persists the setting.
        // ################################################################################################
        private void checkBoxBeep_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxBeep == null) return;
            beepEnabled = checkBoxBeep.Checked;
            if (initializing) return;
            SaveConfig();
        }

        #endregion

        #region Connectivity

        // ################################################################################################
        // Handles the "Check Scope" button; disables capture while checking connectivity and restores UI afterwards.
        // ################################################################################################
        private async void buttonCheckScope_Click(object sender, EventArgs e)
        {
            richTextBoxAction.Text = "Wait, connecting to scope...";
            if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = false;
            if (buttonCheckScope != null) buttonCheckScope.Enabled = false;

            bool ok = await CheckScopeConnectivityAsync();
            if (richTextBoxAction != null)
                richTextBoxAction.Text = ok ? "Ready for capture" : "Cannot connect to scope";

            if (!hotkeyMode)
            {
                if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = true;
                if (buttonCheckScope != null) buttonCheckScope.Enabled = true;
            }
        }

        // ################################################################################################
        // Checks whether the oscilloscope is reachable and responsive using the Scope service.
        // ################################################################################################
        private Task<bool> CheckScopeConnectivityAsync()
        {
            return _scope.CheckConnectivityAsync(scopeIp, scopePort);
        }

        // ################################################################################################
        // Validates that a string can be parsed as an IPv4/IPv6 address.
        // ################################################################################################
        private bool ValidateIp(string value)
        {
            IPAddress addr;
            return IPAddress.TryParse(value, out addr);
        }

        #endregion

        #region Capture workflow

        // ################################################################################################
        // Captures a single screenshot immediately with the current settings.
        // ################################################################################################
        private async void buttonCaptureOnce_Click(object sender, EventArgs e)
        {
            await StartSingleCaptureAsync(null);
        }

        // ################################################################################################
        // Performs a single capture operation (optionally using a capture number and region override), saves to disk, and updates the UI.
        // ################################################################################################
        // <param name="captureNumber">Optional capture sequence number for filename token replacement.</param>
        // <param name="regionOverride">Optional region token override; if null, uses selected region.</param>
        private async Task StartSingleCaptureAsync(int? captureNumber, string regionOverride = null)
        {
            if (captureInProgress)
            {
                Log("Capture already in progress.", LogLevel.Warning);
                return;
            }
            captureInProgress = true;
            if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = false;
            if (buttonCheckScope != null) buttonCheckScope.Enabled = false;
            try
            {
                ApplyComponentFromTextBox(); // ensure text box edits are applied

                string region = regionOverride ?? (comboBoxRegion != null ? comboBoxRegion.SelectedItem as string : null);
                if (string.IsNullOrWhiteSpace(region)) region = "default";
                region = SanitizeForFile(region.Trim());

                string outputFileName = BuildCaptureFileName(captureNumber);
                await CaptureScreenToFileAsync(region, outputFileName);
                if (richTextBoxAction != null)
                    richTextBoxAction.Text = failMode ? "Error occured" : "Ready for capture";
            }
            finally
            {
                if (!hotkeyMode)
                {
                    if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = true;
                    if (buttonCheckScope != null) buttonCheckScope.Enabled = true;
                }
                captureInProgress = false;
            }
        }

        // ################################################################################################
        // Builds the final capture file path using the current filename format and token values (component, number, region, date, time).
        // ################################################################################################
        // <param name="number">Optional explicit number token; if null, resolves from UI.</param>
        private string BuildCaptureFileName(int? number)
        {
            string format = (textBoxFilenameFormat != null && !string.IsNullOrWhiteSpace(textBoxFilenameFormat.Text))
                ? textBoxFilenameFormat.Text
                : filenameFormat;

            string componentVal = (textBoxComponent != null && !string.IsNullOrWhiteSpace(textBoxComponent.Text))
                ? textBoxComponent.Text.Trim()
                : "capture";

            string numberVal;
            if (number.HasValue)
                numberVal = number.Value.ToString();
            else if (textBoxCaptureNumberStart != null && !string.IsNullOrWhiteSpace(textBoxCaptureNumberStart.Text))
                numberVal = textBoxCaptureNumberStart.Text.Trim();
            else
                numberVal = "";

            string regionVal = (comboBoxRegion != null && comboBoxRegion.SelectedItem != null)
                ? comboBoxRegion.SelectedItem.ToString().Trim()
                : "default";

            componentVal = SanitizeForFile(componentVal);
            if (!string.IsNullOrWhiteSpace(numberVal)) numberVal = SanitizeForFile(numberVal);
            regionVal = SanitizeForFile(string.IsNullOrWhiteSpace(regionVal) ? "default" : regionVal);

            string dateVal = DateTime.Now.ToString("yyyyMMdd");
            string timeVal = DateTime.Now.ToString("HHmmss");

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "component", componentVal },
                { "number", numberVal },
                { "region", regionVal },
                { "date", dateVal },
                { "time", timeVal }
            };

            string expanded = Regex.Replace(
                format,
                @"\{(component|number|region|date|time)\}",
                m =>
                {
                    string key = m.Groups[1].Value;
                    string val;
                    return map.TryGetValue(key, out val) ? val : "";
                },
                RegexOptions.IgnoreCase);

            if (string.IsNullOrWhiteSpace(expanded))
                expanded = "capture_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Defensive sanitization of the full filename
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(expanded.Length);
            foreach (char c in expanded)
            {
                if (Array.IndexOf(invalid, c) >= 0 || c < 32)
                    sb.Append('_');
                else
                    sb.Append(c);
            }
            expanded = sb.ToString();
            if (expanded.Length == 0)
                expanded = "capture_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Ensure absolute folder path
            outputFolder = NormalizeOutputFolder(outputFolder);

            return Path.Combine(outputFolder, expanded + ".png");
        }

        // ################################################################################################
        // Executes the capture using the Scope service, updates the preview image and border color, and plays beeps on success/failure.
        // ################################################################################################
        // <param name="regionTag">Sanitized region tag (not used in file path building here but kept for future tagging).</param>
        // <param name="outputFileName">Absolute file path where the image will be saved.</param>
        private Task CaptureScreenToFileAsync(string regionTag, string outputFileName)
        {
            // Ensure absolute/valid path
            outputFolder = NormalizeOutputFolder(outputFolder);

            if (richTextBoxAction != null) richTextBoxAction.Text = "Capturing image";
            ClearPictureBoxImage();
            Beep("before");

            return Task.Run(async () =>
            {
                var result = await _scope.CaptureAsync(scopeIp, scopePort, forceAcquisition, outputFileName);
                if (result.Success)
                {
                    pictureBoxBorderColor = System.Drawing.Color.Green;
                    failMode = false;
                    Beep("after");
                    if (result.PreviewImage != null)
                        SetPictureBoxImage(result.PreviewImage); // ownership transferred from Scope
                }
                else
                {
                    failMode = true;
                    Beep("error");
                    ClearPictureBoxImage();
                }
            });
        }

        // ################################################################################################
        // Draws a colored border around the picture box to indicate last capture status.
        // ################################################################################################
        private void PictureBoxImage_Paint(object sender, PaintEventArgs e)
        {
            var pb = (PictureBox)sender;
            var r = pb.ClientRectangle;
            r.Width -= 1;
            r.Height -= 1;
            using (var pen = new Pen(pictureBoxBorderColor, pictureBoxBorderThickness))
            {
                pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
                e.Graphics.DrawRectangle(pen, r);
            }
        }

        // ################################################################################################
        // Thread-safe image setter for the preview picture box; disposes previous image.
        // ################################################################################################
        private void SetPictureBoxImage(Image img)
        {
            if (pictureBoxImage == null) { img?.Dispose(); return; }
            if (pictureBoxImage.InvokeRequired)
            {
                pictureBoxImage.BeginInvoke(new Action<Image>(SetPictureBoxImage), img);
                return;
            }
            var old = pictureBoxImage.Image;
            pictureBoxImage.Image = img;
            old?.Dispose();
        }

        // ################################################################################################
        // Clears the preview picture box and reverts the border color to "error" (red).
        // ################################################################################################
        private void ClearPictureBoxImage()
        {
            pictureBoxBorderColor = Color.Red;
            if (pictureBoxImage == null) return;
            if (pictureBoxImage.InvokeRequired)
            {
                pictureBoxImage.BeginInvoke(new Action(ClearPictureBoxImage));
                return;
            }
            var old = pictureBoxImage.Image;
            pictureBoxImage.Image = null;
            old?.Dispose();
        }

        // ################################################################################################
        // Plays simple beeps to signal progress: "before" (high), "after" (mid), and "error" (low/long).
        // ################################################################################################
        private void Beep(string type = "before")
        {
            if (!beepEnabled) return;
            try
            {
                Task.Run(delegate
                {
                    try
                    {
                        if (type == "before") Console.Beep(1000, 120);
                        else if (type == "after") Console.Beep(750, 120);
                        else if (type == "error") Console.Beep(250, 750);
                    }
                    catch { }
                });
            }
            catch { }
        }

        #endregion

        #region Hotkey mode and number handling

        // ################################################################################################
        // Starts "continuous capture" hotkey mode. ENTER captures and increments number; ESC exits mode.
        // ################################################################################################
        private void buttonCaptureContinuelsy_Click(object sender, EventArgs e)
        {
            if (hotkeyMode)
            {
                Log("Already in capture mode. Press [ESC] to exit.", LogLevel.Warning);
                return;
            }
            if (!TryInitializeNumberRange()) return;
            originalNumberStart = numberStart;
            hotkeyMode = true;
            numberRangeActive = true;

            if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = false;
            if (buttonCheckScope != null) buttonCheckScope.Enabled = false;

            UpdateNumberStatusText();
            Log("Capture mode active. Press [ENTER] to capture, [ESC] to stop.", LogLevel.Highlight);

            FocusNumberTextboxCaretToEnd();
        }

        // ################################################################################################
        // Exits hotkey mode and restores UI state.
        // ################################################################################################
        private void DisableHotkeyMode()
        {
            if (!hotkeyMode) return;
            hotkeyMode = false;
            numberRangeActive = false;
            if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = true;
            if (buttonCheckScope != null) buttonCheckScope.Enabled = true;
            Log("Capture mode disabled.", LogLevel.Notice);
        }

        // ################################################################################################
        // Global key handler:
        // + / - adjust timebase, * toggles snapshot, / resumes, ENTER captures, ESC exits hotkey mode.
        // ################################################################################################
        private async void Form_KeyDown(object sender, KeyEventArgs e)
        {
            // Only react to keyboard commands while in capture mode.
            if (!hotkeyMode)
            {
                // Allow starting capture mode from keyboard.
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    buttonCaptureContinuelsy_Click(this, EventArgs.Empty);
                }
                return;
            }

            // -------- Capture mode hotkeys --------

            // Timebase zoom
            if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await AdjustTimebaseAsync(false); // decrease timespan (zoom-in)
                return;
            }

            if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await AdjustTimebaseAsync(true); // increase timespan (zoom-out)
                return;
            }

            // Snapshot toggle
            if (e.KeyCode == Keys.Multiply || (e.KeyCode == Keys.D8 && e.Shift))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await SnapshotToggleAsync();
                return;
            }

            // Resume acquisition
            if (e.KeyCode == Keys.Divide || e.KeyCode == Keys.OemQuestion)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await ResumeAcquisitionAsync();
                return;
            }

            // Trigger level up/down
            if (e.KeyCode == Keys.Up)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await AdjustTriggerLevelAsync(true);   // raise trigger level
                return;
            }

            if (e.KeyCode == Keys.Down)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await AdjustTriggerLevelAsync(false);  // lower trigger level
                return;
            }

            // Capture and exit
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                if (numberRangeActive)
                {
                    RefreshNumberRangeFromTextBoxes(false);

                    int currentNumber = numberStart;
                    await StartSingleCaptureAsync(currentNumber, null);

                    numberStart++;
                    if (textBoxCaptureNumberStart != null) textBoxCaptureNumberStart.Text = numberStart.ToString();
                    UpdateNumberStatusText();
                    FocusNumberTextboxCaretToEnd();
                }
                else
                {
                    await StartSingleCaptureAsync(null);
                }
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                DisableHotkeyMode();
                if (richTextBoxAction != null) richTextBoxAction.Text = "Ready for capture";
                return;
            }
        }

        // ################################################################################################
        // Initializes the continuous numbering from the textbox; defaults to 1 on invalid input.
        // ################################################################################################
        private bool TryInitializeNumberRange()
        {
            int startParsed;
            var raw = textBoxCaptureNumberStart.Text.Trim();

            if (!int.TryParse(raw, out startParsed) || startParsed <= 0)
            {
                Log("Invalid start number. Defaulting to 1.", LogLevel.Warning);
                startParsed = 1;
                textBoxCaptureNumberStart.Text = "1";
            }

            numberStart = startParsed;
            return true;
        }

        // ################################################################################################
        // Attempts to refresh the current number from the textbox; optionally logs on invalid input.
        // ################################################################################################
        private bool RefreshNumberRangeFromTextBoxes(bool logOnError = true)
        {
            int startParsed;
            var rawStart = textBoxCaptureNumberStart.Text.Trim();

            if (int.TryParse(rawStart, out startParsed) && startParsed > 0)
            {
                numberStart = startParsed;
            }
            else
            {
                if (logOnError) Log("Invalid number format (non-numeric). Keeping current number.", LogLevel.Warning);
            }

            return true;
        }

        // ################################################################################################
        // Updates the "action" RTF field to show current status and next capture number.
        // ################################################################################################
        private void UpdateNumberStatusText()
        {
            if(richTextBoxAction == null) return;

            string comp = EscapeRtf(component ?? "");
            string rtf =
                "{\\rtf1\\ansi" +
                "\\fs28 Ready to capture; [\\b " + comp + "\\b0] number [\\b " + numberStart + "\\b0]\\line " +
                "Press [ENTER] to capture, [ESC] to stop." +
                "}";

            richTextBoxAction.Rtf = rtf;
        }

        private static string EscapeRtf(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
        }

        // ################################################################################################
        // Moves focus to the number textbox and places the caret at the end without selecting text.
        // ################################################################################################
        private void FocusNumberTextboxCaretToEnd()
        {
            if (textBoxCaptureNumberStart == null) return;
            BeginInvoke(new Action(() =>
            {
                try
                {
                    if (textBoxCaptureNumberStart.CanFocus)
                        textBoxCaptureNumberStart.Focus();
                    textBoxCaptureNumberStart.SelectionStart = textBoxCaptureNumberStart.TextLength;
                    textBoxCaptureNumberStart.SelectionLength = 0;
                }
                catch { /* ignore focus timing issues */ }
            }));
        }

        #endregion

        #region Scope controls: timebase, snapshot, resume

        // ################################################################################################
        // Adjusts the trigger level up/down using the Scope service.
        // ################################################################################################
        private Task AdjustTriggerLevelAsync(bool increase)
        {
            return _scope.AdjustTriggerLevelAsync(scopeIp, scopePort, increase);
        }

        // ################################################################################################
        // Adjusts the instrument's timebase (seconds/div) up/down using the Scope service.
        // ################################################################################################
        // <param name="increase">True to increase timespan (zoom out), false to decrease (zoom in).</param>
        private Task AdjustTimebaseAsync(bool increase)
        {
            return _scope.AdjustTimebaseAsync(scopeIp, scopePort, increase);
        }

        // ################################################################################################
        // Toggles snapshot: if running, STOP (freeze); if stopped, acquire one fresh waveform and stop again.
        // ################################################################################################
        private Task SnapshotToggleAsync()
        {
            return _scope.SnapshotToggleAsync(scopeIp, scopePort);
        }

        // ################################################################################################
        // Resumes continuous acquisition (RUN), using vendor-specific fallbacks when needed.
        // ################################################################################################
        private Task ResumeAcquisitionAsync()
        {
            return _scope.ResumeAcquisitionAsync(scopeIp, scopePort);
        }

        #endregion

        #region UI helpers and logging

        // ################################################################################################
        // Writes a log entry through the logger service with a specified severity level.
        // ################################################################################################
        private void Log(string message, LogLevel level) => _logger.Log(message, level);

        // ################################################################################################
        // Appends a colored line to the UI log with timestamp and level-dependent color; filters Debug if unchecked.
        // ################################################################################################
        private void AppendColoredLine(string message, LogLevel level)
        {
            if (richTextBoxLog == null) return;

            if (level == LogLevel.Debug && (checkBoxDebug == null || !checkBoxDebug.Checked))
                return;

            Color color;
            switch (level)
            {
                case LogLevel.Highlight: color = Color.White; break;
                case LogLevel.Warning: color = Color.Khaki; break;
                case LogLevel.Error: color = Color.LightPink; break;
                case LogLevel.Notice: color = Color.SkyBlue; break;
                case LogLevel.Debug: color = Color.DarkSeaGreen; break;
                default: color = Color.LightGreen; break;
            }
            string line = $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}";
            int start = richTextBoxLog.TextLength;
            richTextBoxLog.AppendText(line);
            richTextBoxLog.Select(start, line.Length);
            richTextBoxLog.SelectionColor = color;
            richTextBoxLog.SelectionLength = 0;
            richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
            richTextBoxLog.ScrollToCaret();
        }

        // ################################################################################################
        // Sanitizes a string for safe use in filenames (replaces invalid characters and whitespace with underscores).
        // ################################################################################################
        private string SanitizeForFile(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "default";
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c < 32 || Array.IndexOf(invalid, c) >= 0) sb.Append('_');
                else if (char.IsWhiteSpace(c)) sb.Append('_');
                else sb.Append(c);
            }
            string cleaned = sb.ToString();
            while (cleaned.Contains("__")) cleaned = cleaned.Replace("__", "_");
            cleaned = cleaned.Trim('_');
            return string.IsNullOrEmpty(cleaned) ? "default" : cleaned;
        }

        // ################################################################################################
        // Sets application labels to reflect the executable's version and logs it.
        // ################################################################################################
        private void InitializeVersionLabel()
        {
            Assembly assemblyInfo = Assembly.GetExecutingAssembly();
            string assemblyVersion = FileVersionInfo.GetVersionInfo(assemblyInfo.Location).FileVersion;
            string year = assemblyVersion.Substring(0, 4);
            string month = assemblyVersion.Substring(5, 2);
            string day = assemblyVersion.Substring(8, 2);
            string rev = assemblyVersion.Substring(11); // ignored in release builds

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
            day = day.TrimStart(new char[] { '0' });
            day = day.TrimEnd(new char[] { '.' });
            string date = year + "-" + month + "-" + day;
            if (rev != "0" && rev != "1")
            {
                rev = " (rev. " + rev + ")";
            }
            else
            {
                rev = "";
            }
            versionThis = date + rev;
            labelProductVersion.Text = "Version " + versionThis;
            Log("Application version: " + versionThis, LogLevel.Notice);
        }

        // ################################################################################################
        // Populates the "Help" rich text box with general usage instructions and keyboard shortcuts.
        // Uses light-gray highlighted runs to simulate keyboard "keycaps" (ENTER, +, -, etc.).
        // ################################################################################################
        private void LoadHelpText()
        {
            // RTF header with font and color tables
            var sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\deff0");
            sb.Append(@"{\fonttbl{\f0 Segoe UI;}{\f1 Consolas;}}");
            // colortbl: [0]=auto; [1]=text (black); [2]=keycap bg (light gray)
            sb.Append(@"{\colortbl ;\red0\green0\blue0;\red230\green234\blue238;}");
            sb.Append(@"\fs22 "); // 11pt base size

            sb.Append(@"{\fs28{\b Rigol}}\line ");
            sb.Append(@"Typical port is {\b 5555} (I am actually not sure on this - is this typical?).\line ");
            sb.Append(@"\line ");

            sb.Append(@"{\fs28{\b Siglent}}\line ");
            sb.Append(@"Typical port is {\b 5025} (I am actually not sure on this - is this typical?).\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Variables to use in filename format}}\line ");
            sb.Append(@"    * "+ KeycapRtf("{Region}") + @" is either PAL or NTSC\line ");
            sb.Append(@"    * "+ KeycapRtf("{Component}") + @" is the component/label you are doing an measurement on\line ");
            sb.Append(@"    * "+ KeycapRtf("{Number}") + @" is whatever number you may be meassuring (e.g. IC pin number 7)\line ");
            sb.Append(@"    * "+ KeycapRtf("{Date}") + @" is YYYYMMDD - e.g. 20251231\line ");
            sb.Append(@"    * "+ KeycapRtf("{Time}") + @" is HHMMSS - e.g. 235959\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b General}}\line ");
            sb.Append(@"In capture mode you can change the variables on-the-fly, and it will be used for the next saved file.\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Keyboard commands (in capture mode)}}\line ");
            sb.Append("    * " + KeycapRtf("ENTER") + " at application launch will start capture mode\\line ");
            sb.Append("    * " + KeycapRtf("ENTER") + " in capture mode will save image from scope to a file with a specific filename format\\line ");
            sb.Append("    * " + KeycapRtf("ESCAPE") + " in capture mode will exit capture mode\\line ");
            sb.Append("    * " + KeycapRtf("+") + " to decrease timespan (zoom-in)\\line ");
            sb.Append("    * " + KeycapRtf("-") + " to increase timespan (zoom-out)\\line ");
            sb.Append("    * " + KeycapRtf("*") + " to STOP acquisition on scope\\line ");
            sb.Append("    * " + KeycapRtf("*") + " to take a new snapshot on scope\\line ");
            sb.Append("    * " + KeycapRtf("/") + " to RESUME acquisition on scope\\line ");
            sb.Append("    * " + KeycapRtf("ARROW UP") + " to raise trigger level 0.25V\\line ");
            sb.Append("    * " + KeycapRtf("ARROW DOWN") + " to lower trigger level 0.25V\\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Troubleshoot no connectivity to you oscilloscope}}\line ");
            sb.Append(@"If you do not get any connection to your oscilloscope, then please do validate that your computer can actually connect to the oscilloscope over network. You can do this by this simple commandline prompt, but it does require that you do have the "+ KeycapRtf("telnet") + @" client installed (can be installed from ""Programs and Feaures > Turn Windows features on or off"" from Windows Control Panel):\line\line ");
            sb.Append(@"    "+ KeycapRtf("telnet 192.168.0.100 5555") + @"\line\line ");
            sb.Append(@"If this results in a black screen and no errors, then you do have connectivity. You of course needs to adapt this for your scope, so it will have another IP address and probably also another port instead of ""5555"". Maybe also your scope has a web interface, so you can try also accessing it on its IP addresses for both HTTP and HTTPS.\line ");
            sb.Append(@"Also, it might help power cycling your scope.\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Standard and protocol used:}}\line ");
            sb.Append(@"    * IEEE 488.2 standard\line ");
            sb.Append(@"    * SCPI socket protocol\line ");
            sb.Append(@"\line");

            sb.Append(@"{\fs28{\b Confirmed working on following oscilloscopes}}\line ");
            sb.Append(@"    * Rigol DS2202A\line ");
            sb.Append(@"    * Siglent SDS 1104X - E\line ");
            sb.Append(@"    * Siglent SDS 1204X - E\line ");
            sb.Append(@"\line");

            sb.Append("}");

            richTextBoxHelp.Rtf = sb.ToString();
        }

        // Returns an RTF run that looks like a keycap: light gray background + small padding.
        // Uses the color table defined in LoadHelpText(): text=cf1 (black), key bg=highlight2.
        // Padding is simulated using non-breaking spaces (~) on both sides.
        private string KeycapRtf(string text)
        {
            // base font for keys a tad smaller than body
            var inner = $"\\f0\\fs20\\cf1\\highlight2\\~{EscapeRtf(text)}\\~\\highlight0";
            return "{" + inner + "}";
        }

        #endregion

        #region About and version check

        // ################################################################################################
        // Opens clicked hyperlinks in the default browser; normalizes to HTTPS if missing.
        // ################################################################################################
        private void richTextBoxAbout_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                string url = e.LinkText?.Trim();
                if (string.IsNullOrEmpty(url)) return;

                if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url.TrimStart('/');
                }

                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
                Log("Opened URL: " + url, LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Log("Failed to open URL: " + ex.Message, LogLevel.Error);
            }
        }

        // Populates the "About" rich text box and makes the quoted sentence italic.
        private void LoadAboutText()
        {
            if (richTextBoxAbout == null) return;

            richTextBoxAbout.ReadOnly = true;
            richTextBoxAbout.DetectUrls = true; // keep URLs clickable

            var sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi ");
            sb.Append(EscapeRtf("This is a simple application, which has been designed for one purpose only:"));
            sb.Append(@"\line\line ");

            // Italicized quoted line
            string quoted = "\"Make it easier and faster for me to capture oscilloscope measurements, when creating oscilloscope baseline images for my other project; Commodore Repair Toolbox.\"";
            sb.Append(@"{\i " + EscapeRtf(quoted) + "}");
            sb.Append(@"\line\line ");

            sb.Append(EscapeRtf("It has been designed to work specifically with my Rigol DS2202A oscilloscope, but as it uses the SCPI socket protocol, then it should also work for other oscilloscopes also - but this is where I need some feedback, as I only have access to my own scope :-) The goal is not to make this a full-blown \"Swiss army knife\" that suits everyones need, but if can help someone other than myself, then I am happy to make this available."));
            sb.Append(@"\line\line ");

            sb.Append(EscapeRtf("You can check for a newer version on GitHub, https://github.com/HovKlan-DH/Oscilloscope-Network-Capture"));
            sb.Append(@"\line\line ");

            sb.Append(EscapeRtf("Kind regards,"));
            sb.Append(@"\line\line ");

            sb.Append(EscapeRtf("Dennis Helligsø, dennis@commodore-repair-toolbox.dk"));
            sb.Append("}");

            richTextBoxAbout.Rtf = sb.ToString();
        }

        // ################################################################################################
        // Queries the remote service for a newer version and shows a UI hint if one is available.
        // ################################################################################################
        private async Task CheckOnlineVersionAsync()
        {
            try
            {
                using (var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) })
                {
                    var baseUri = new Uri(oncPage);
                    var reqUri = new Uri(baseUri, oncPageAutoUpdate ?? "");
                    var content = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string,string>("control", "ONC"),
            });

                    http.DefaultRequestHeaders.UserAgent.ParseAdd("ONC " + (versionThis ?? ""));

                    var resp = await http.PostAsync(reqUri, content).ConfigureAwait(true);
                    if (!resp.IsSuccessStatusCode) return;

                    var body = (await resp.Content.ReadAsStringAsync().ConfigureAwait(true))?.Trim() ?? "";

                    // Accept lines like: "Version 2025-September-19 (rev. 1)" or "Version: 2025-09-19"
                    var m = Regex.Match(body, @"^Version\s*:?\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (m.Success)
                    {
                        var onlineAvailableVersion = m.Groups[1].Value.Trim();
                        if (!string.Equals(onlineAvailableVersion, versionThis, StringComparison.OrdinalIgnoreCase))
                        {
                            if (labelNewVersionAvailable != null)
                                labelNewVersionAvailable.Visible = true;
                        }
                    }
                }
            }
            catch
            {
                // Silent failure is acceptable for optional check
            }
        }

        #endregion
    }
}