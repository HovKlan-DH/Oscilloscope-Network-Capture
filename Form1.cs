using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Oscilloscope_Network_Capture
{
    public partial class Form1 : Form
    {
        private string scopeIp = "192.168.0.100";
        private int scopePort = 5555;
        private string component = "U1";
        private string outputFolder = "output";
        private string filenameFormat = "{Component}_{Pin}_{Region}";
        private bool beepEnabled = true;

        private readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Oscilloscope Network Capture.cfg");

        private Color pictureBoxBorderColor = Color.Red;
        private readonly int pictureBoxBorderThickness = 2;
        private bool initializing = false;

        // Hotkey mode
        private bool hotkeyMode = false;
        private bool captureInProgress = false;
        private bool failMode = false;

        // Pin range
        private int pinStart = 1;
        private int pinEnd = 40;
        private int originalPinStart;
        private int originalPinEnd;
        private bool pinRangeActive = false;

        private enum LogLevel { Info, Warning, Error, Notice }

        public Form1()
        {
            initializing = true;
            InitializeComponent();

            // Version label
            // ---
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
            labelProductVersion.Text = "Version " + date + rev;

            if (textBoxIp != null) textBoxIp.Text = scopeIp;

            if (comboBoxRegion != null)
            {
                comboBoxRegion.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
                if (comboBoxRegion.Items.Count > 0)
                    comboBoxRegion.SelectedIndex = 0;
            }

            checkBoxBeep.CheckedChanged += checkBoxBeep_CheckedChanged;

            LoadConfig();
            EnsureLog();

            textBoxComponent.Text = component;
            textBoxFilenameFormat.Text = filenameFormat;

            textBoxPort.Text = scopePort.ToString();
            textBoxPort.Leave += textBoxPort_Leave;
            textBoxPort.KeyDown += textBoxPort_KeyDown;
            textBoxFilenameFormat.Leave += textBoxFilenameFormat_Leave;
            textBoxFilenameFormat.KeyDown += textBoxFilenameFormat_KeyDown;
            buttonCaptureOnce.Click += button1_Click;
            buttonCaptureContinuelsy.Click += button2_Click;
            buttonCheckScope.Click += button3_Click;

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            pictureBoxImage.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxImage.BorderStyle = BorderStyle.None;
            pictureBoxImage.Paint += PictureBox1_Paint;
            pictureBoxIcon.SizeMode = PictureBoxSizeMode.Zoom;
            textBoxIp.Leave += textBoxIp_Leave;
            textBoxIp.KeyDown += textBoxIp_KeyDown;
            textBoxCapturePin.KeyDown += textBox5_KeyDown;
            textBoxCapturePinStart.KeyDown += textBoxPinRange_KeyDown;
            textBoxCapturePinEnd.KeyDown += textBoxPinRange_KeyDown;

            checkBoxBeep.Checked = beepEnabled;

            Log("Ready.", LogLevel.Info);
            richTextBoxAction.Text = "Ready for capture";
            initializing = false;
        }

        // ---- Filename format handlers ----
        private void textBoxFilenameFormat_Leave(object sender, EventArgs e) => ApplyFilenameFormatFromTextBox();
        private void textBoxFilenameFormat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyFilenameFormatFromTextBox();
                e.SuppressKeyPress = true;
            }
        }
        private void ApplyFilenameFormatFromTextBox()
        {
            if (textBoxFilenameFormat == null) return;
            string raw = textBoxFilenameFormat.Text;
            if (string.IsNullOrWhiteSpace(raw))
            {
                Log("Filename format empty; reverting to default.", LogLevel.Warning);
                filenameFormat = "{Component}_{Pin}_{Region}";
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

        // ---- Port textbox handlers ----
        private void textBoxPort_Leave(object sender, EventArgs e) => ApplyPortFromTextBox();
        private void textBoxPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyPortFromTextBox();
                e.SuppressKeyPress = true;
            }
        }
        private void ApplyPortFromTextBox()
        {
            if (textBoxPort == null) return;
            string raw = textBoxPort.Text.Trim();
            int newPort;
            if (!int.TryParse(raw, out newPort) || newPort < 1 || newPort > 65535)
            {
                Log("Invalid port (must be 1..65535): " + raw, LogLevel.Warning);
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

        // ================= CONFIG LOAD / SAVE =================
        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(configPath)) return;
                string[] lines = File.ReadAllLines(configPath);
                bool hasKv = false;
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        hasKv = true;
                        map[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                    }
                }
                if (hasKv)
                {
                    string ip;
                    if (map.TryGetValue("IP", out ip) && ValidateIp(ip))
                    {
                        scopeIp = ip;
                        if (textBoxIp != null) textBoxIp.Text = scopeIp;
                    }
                    string region;
                    if (comboBoxRegion != null && map.TryGetValue("Region", out region) && !string.IsNullOrWhiteSpace(region))
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
                    string portStr;
                    if (map.TryGetValue("Port", out portStr))
                    {
                        int p;
                        if (int.TryParse(portStr, out p) && p > 0 && p < 65536)
                        {
                            scopePort = p;
                            if (textBoxPort != null) textBoxPort.Text = scopePort.ToString();
                        }
                    }
                    string fmt;
                    if (map.TryGetValue("FilenameFormat", out fmt) && !string.IsNullOrWhiteSpace(fmt))
                    {
                        filenameFormat = fmt;
                        if (textBoxFilenameFormat != null) textBoxFilenameFormat.Text = filenameFormat;
                    }
                    string beepStr;
                    if (map.TryGetValue("Beep", out beepStr))
                    {
                        beepEnabled = (beepStr == "1") ||
                                      beepStr.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                      beepStr.Equals("yes", StringComparison.OrdinalIgnoreCase);
                        if (checkBoxBeep != null) checkBoxBeep.Checked = beepEnabled;
                    }
                    Log($"Loaded config: IP={scopeIp}, Port={scopePort}, Region={(comboBoxRegion?.SelectedItem as string) ?? ""}, FilenameFormat={filenameFormat}, Beep={(beepEnabled ? 1 : 0)}", LogLevel.Info);
                }
                else
                {
                    // Legacy single-line (IP only)
                    string txt = string.Join("", lines).Trim();
                    if (!string.IsNullOrWhiteSpace(txt) && ValidateIp(txt))
                    {
                        scopeIp = txt;
                        if (textBoxIp != null) textBoxIp.Text = scopeIp;
                        Log("Loaded legacy IP [" + scopeIp + "] from configuration file.", LogLevel.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Warning: Could not load configuration file: " + ex.Message, LogLevel.Warning);
            }
        }

        private void SaveConfig()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("IP=" + scopeIp);
                sb.AppendLine("Port=" + scopePort);
                string region = comboBoxRegion != null ? (comboBoxRegion.SelectedItem as string ?? "").Trim() : "";
                sb.AppendLine("Region=" + region);
                sb.AppendLine("FilenameFormat=" + filenameFormat);
                sb.AppendLine("Beep=" + (beepEnabled ? "1" : "0"));
                File.WriteAllText(configPath, sb.ToString());
                Log($"Saved config: IP={scopeIp}, Port={scopePort}, Region={region}, FilenameFormat={filenameFormat}, Beep={(beepEnabled ? 1 : 0)}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log("Warning: Could not save configuration file: " + ex.Message, LogLevel.Warning);
            }
        }

        // ================= BASIC UI EVENTS =================
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            SaveConfig();
        }

        private void checkBoxBeep_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxBeep == null) return;
            beepEnabled = checkBoxBeep.Checked;
            if (initializing) return;
            SaveConfig();
        }

        private void textBoxIp_Leave(object sender, EventArgs e) => ApplyIpFromTextBox();
        private void textBoxIp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyIpFromTextBox();
                e.SuppressKeyPress = true;
            }
        }
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
                Log("Invalid IP format: " + candidate, LogLevel.Warning);
                textBoxIp.Text = scopeIp;
            }
        }
        private bool ValidateIp(string value)
        {
            IPAddress addr;
            return IPAddress.TryParse(value, out addr);
        }

        // ================= CONNECTIVITY CHECK (NO ICMP) =================
        private async void button3_Click(object sender, EventArgs e)
        {
            if (buttonCaptureOnce != null) buttonCaptureOnce.Enabled = false;
            if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = false;
            if (buttonCheckScope != null) buttonCheckScope.Enabled = false;
            await CheckScopeConnectivityAsync();
            if (!hotkeyMode)
            {
                if (buttonCaptureOnce != null) buttonCaptureOnce.Enabled = true;
                if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = true;
                if (buttonCheckScope != null) buttonCheckScope.Enabled = true;
            }
        }

        private async Task CheckScopeConnectivityAsync()
        {
            string host = scopeIp;
            int port = scopePort;
            const int connectTimeoutMs = 5000;
            const int ioTimeoutMs = 60000;

            if (buttonCheckScope != null) buttonCheckScope.Enabled = false;
            try
            {
                Log("Attempting TCP connectivity.", LogLevel.Notice);

                if (!TestTcpPort(host, port, 600))
                    Log($"Port {port} preliminary probe failed (will still attempt full connect).", LogLevel.Warning);
                else
                    Log($"Port {port} preliminary probe succeeded.", LogLevel.Info);

                Log("Opening SCPI session: " + host + ":" + port, LogLevel.Info);

                await Task.Run(() =>
                {
                    try
                    {
                        using (var scpi = RawScpiClient.Connect(host, port, connectTimeoutMs, ioTimeoutMs))
                        {
                            scpi.DrainInput();
                            scpi.ClearStatus();

                            string idn = scpi.QueryLine("*IDN?");
                            Log("IDN: " + (idn ?? "").Trim(), LogLevel.Info);

                            if (!string.IsNullOrWhiteSpace(idn))
                            {
                                var p = idn.Split(',');
                                if (p.Length >= 4)
                                {
                                    Log("Vendor: " + p[0], LogLevel.Info);
                                    Log("Model: " + p[1], LogLevel.Info);
                                    Log("Serial: " + p[2], LogLevel.Info);
                                    Log("Firmware: " + p[3], LogLevel.Info);
                                }
                            }

                            for (int i = 0; i < 6; i++)
                            {
                                var line = scpi.TryQuery(":SYST:ERR?", timeoutMs: 1200);
                                if (string.IsNullOrWhiteSpace(line)) break;
                                if (line.StartsWith("0,", StringComparison.Ordinal)) break;
                                Log("Instrument error: " + line.Trim(), LogLevel.Warning);
                            }
                        }
                    }
                    catch (TimeoutException tex)
                    {
                        Log("Connectivity check failed (timeout): " + tex.Message, LogLevel.Error);
                        ClearPictureBoxImage();
                    }
                    catch (IOException ioex)
                    {
                        Log("Connectivity check failed (I/O): " + ioex.Message, LogLevel.Error);
                        ClearPictureBoxImage();
                    }
                    catch (Exception ex)
                    {
                        Log("Connectivity check failed: " + ex.Message, LogLevel.Error);
                        ClearPictureBoxImage();
                    }
                });
            }
            finally
            {
                if (buttonCheckScope != null) buttonCheckScope.Enabled = true;
            }
        }

        // ================= HOTKEY / PIN MODE =================
        private void button2_Click(object sender, EventArgs e)
        {
            if (hotkeyMode)
            {
                Log("Already in hotkey mode. Press [ESC] to exit.", LogLevel.Warning);
                return;
            }
            if (!TryInitializePinRange()) return;
            originalPinStart = pinStart;
            originalPinEnd = pinEnd;
            hotkeyMode = true;
            pinRangeActive = true;

            if (buttonCaptureOnce != null) buttonCaptureOnce.Enabled = false;
            if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = false;
            if (buttonCheckScope != null) buttonCheckScope.Enabled = false;

            UpdatePinStatusText();
            Log("Hotkey mode active. Press [ENTER] to capture current pin, [ESC] to stop.", LogLevel.Notice);
        }

        private bool TryInitializePinRange()
        {
            if (textBoxCapturePinStart == null || textBoxCapturePinEnd == null)
            {
                Log("Pin range controls not found.", LogLevel.Error);
                return false;
            }
            int startParsed, endParsed;
            if (!int.TryParse(textBoxCapturePinStart.Text.Trim(), out startParsed) ||
                !int.TryParse(textBoxCapturePinEnd.Text.Trim(), out endParsed) ||
                startParsed <= 0 || endParsed <= 0 || endParsed < startParsed)
            {
                Log("Invalid pin range. Provide positive integers with start <= end.", LogLevel.Warning);
                return false;
            }
            pinStart = startParsed;
            pinEnd = endParsed;
            return true;
        }

        private void UpdatePinStatusText()
        {
            if (richTextBoxAction == null) return;
            if (pinStart <= pinEnd)
                richTextBoxAction.Text = $"Ready to capture pin {pinStart} of {pinEnd}\r\nPress [ENTER] to capture, [ESC] to stop.";
            else
                richTextBoxAction.Text = "Capture completed";
        }

        private void DisableHotkeyMode()
        {
            if (!hotkeyMode) return;
            hotkeyMode = false;
            pinRangeActive = false;
            if (buttonCaptureOnce != null) buttonCaptureOnce.Enabled = true;
            if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = true;
            if (buttonCheckScope != null) buttonCheckScope.Enabled = true;
            Log("Hotkey capture mode disabled.", LogLevel.Notice);
        }

        private async void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!hotkeyMode) return;
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (pinRangeActive)
                {
                    if (!RefreshPinRangeFromTextBoxes())
                    {
                        if (richTextBoxAction != null) richTextBoxAction.Text = "Invalid pin range";
                        return;
                    }
                    if (pinStart > pinEnd)
                    {
                        Log("Pin range already complete.", LogLevel.Notice);
                        DisableHotkeyMode();
                        if (richTextBoxAction != null) richTextBoxAction.Text = "Capture completed";
                        return;
                    }
                    int currentPin = pinStart;
                    await StartSingleCaptureAsync(currentPin, null);
                    if (currentPin == pinEnd)
                    {
                        Log("Finished capturing all pins.", LogLevel.Notice);
                        if (richTextBoxAction != null) richTextBoxAction.Text = "Capture completed";
                        pinStart = originalPinStart;
                        pinEnd = originalPinEnd;
                        if (textBoxCapturePinStart != null) textBoxCapturePinStart.Text = pinStart.ToString();
                        if (textBoxCapturePinEnd != null) textBoxCapturePinEnd.Text = pinEnd.ToString();
                        Log($"Pin range reset to original [{pinStart}..{pinEnd}].", LogLevel.Info);
                        DisableHotkeyMode();
                        return;
                    }
                    pinStart++;
                    if (textBoxCapturePinStart != null) textBoxCapturePinStart.Text = pinStart.ToString();
                    UpdatePinStatusText();
                }
                else
                {
                    await StartSingleCaptureAsync(null);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                DisableHotkeyMode();
                if (richTextBoxAction != null) richTextBoxAction.Text = "Ready for capture";
            }
        }

        private bool RefreshPinRangeFromTextBoxes(bool logOnError = true)
        {
            if (textBoxCapturePinStart == null || textBoxCapturePinEnd == null)
            {
                if (logOnError) Log("Pin range controls not found.", LogLevel.Error);
                return false;
            }
            int startParsed, endParsed;
            if (!int.TryParse(textBoxCapturePinStart.Text.Trim(), out startParsed) ||
                !int.TryParse(textBoxCapturePinEnd.Text.Trim(), out endParsed))
            {
                if (logOnError) Log("Invalid pin numbers (non-numeric).", LogLevel.Warning);
                return false;
            }
            if (startParsed <= 0 || endParsed <= 0)
            {
                if (logOnError) Log("Pin numbers must be positive.", LogLevel.Warning);
                return false;
            }
            pinStart = startParsed;
            pinEnd = endParsed;
            return true;
        }

        // ================= SINGLE CAPTURE =================
        private async void button1_Click(object sender, EventArgs e)
        {
            await StartSingleCaptureAsync(null);
        }

        private async Task StartSingleCaptureAsync(int? capturePinNumber, string regionOverride = null)
        {
            if (captureInProgress)
            {
                Log("Capture already in progress.", LogLevel.Warning);
                return;
            }
            captureInProgress = true;
            if (buttonCaptureOnce != null) buttonCaptureOnce.Enabled = false;
            if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = false;
            if (buttonCheckScope != null) buttonCheckScope.Enabled = false;
            try
            {
                string region = regionOverride ?? (comboBoxRegion != null ? comboBoxRegion.SelectedItem as string : null);
                if (string.IsNullOrWhiteSpace(region)) region = "default";
                region = SanitizeForFile(region.Trim());

                string outputFileName = BuildCaptureFileName(capturePinNumber);
                await CaptureScreenToFileAsync(region, outputFileName);
                if (richTextBoxAction != null)
                    richTextBoxAction.Text = failMode ? "Error occured" : "Ready for capture";
            }
            finally
            {
                if (!hotkeyMode)
                {
                    if (buttonCaptureOnce != null) buttonCaptureOnce.Enabled = true;
                    if (buttonCaptureContinuelsy != null) buttonCaptureContinuelsy.Enabled = true;
                    if (buttonCheckScope != null) buttonCheckScope.Enabled = true;
                }
                captureInProgress = false;
            }
        }

        // Filename construction preserves user format verbatim (only substitutes variables and removes illegal chars)
        private string BuildCaptureFileName(int? pinNumber)
        {
            string format = (textBoxFilenameFormat != null && !string.IsNullOrWhiteSpace(textBoxFilenameFormat.Text))
                ? textBoxFilenameFormat.Text
                : filenameFormat;

            string componentVal = (textBoxComponent != null && !string.IsNullOrWhiteSpace(textBoxComponent.Text))
                ? textBoxComponent.Text.Trim()
                : "capture";

            string pinVal;
            if (pinNumber.HasValue)
                pinVal = pinNumber.Value.ToString();
            else if (textBoxCapturePin != null && !string.IsNullOrWhiteSpace(textBoxCapturePin.Text))
                pinVal = textBoxCapturePin.Text.Trim();
            else if (textBoxCapturePinStart != null && !string.IsNullOrWhiteSpace(textBoxCapturePinStart.Text))
                pinVal = textBoxCapturePinStart.Text.Trim();
            else
                pinVal = "";

            string regionVal = (comboBoxRegion != null && comboBoxRegion.SelectedItem != null)
                ? comboBoxRegion.SelectedItem.ToString().Trim()
                : "default";

            componentVal = SanitizeForFile(componentVal);
            if (!string.IsNullOrWhiteSpace(pinVal)) pinVal = SanitizeForFile(pinVal);
            regionVal = SanitizeForFile(string.IsNullOrWhiteSpace(regionVal) ? "default" : regionVal);

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "component", componentVal },
                { "pin", pinVal },
                { "region", regionVal }
            };

            string expanded = Regex.Replace(
                format,
                @"\{(component|pin|region)\}",
                m =>
                {
                    string key = m.Groups[1].Value;
                    string val;
                    return map.TryGetValue(key, out val) ? val : "";
                },
                RegexOptions.IgnoreCase);

            if (string.IsNullOrWhiteSpace(expanded))
                expanded = "capture_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Replace invalid filename chars only, preserve user's formatting
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

            return Path.Combine(outputFolder, expanded + ".png");
        }

        // ================= MULTI-VENDOR SCREENSHOT LOGIC (NO ICMP) =================
        private Task CaptureScreenToFileAsync(string regionTag, string outputFileName)
        {
            string host = scopeIp;
            int port = scopePort;
            const int connectTimeoutMs = 5000;
            const int ioTimeoutMs = 60000;

            if (richTextBoxAction != null) richTextBoxAction.Text = "Capturing image";

            return Task.Run(() =>
            {
                ClearPictureBoxImage();
                Beep("before");
                bool success = false;

                try
                {
                    Log($"Connecting {host}:{port}", LogLevel.Info);
                    using (var scpi = RawScpiClient.Connect(host, port, connectTimeoutMs, ioTimeoutMs))
                    {
                        scpi.DrainInput();
                        scpi.ClearStatus();

                        string idn = scpi.TryQuery("*IDN?", timeoutMs: 2500);
                        if (string.IsNullOrWhiteSpace(idn))
                            Log("No *IDN? response.", LogLevel.Warning);
                        else
                            Log("IDN: " + idn.Trim(), LogLevel.Info);

                        string vendorUpper = "";
                        if (!string.IsNullOrWhiteSpace(idn))
                            vendorUpper = idn.Split(',')[0].Trim().ToUpperInvariant();

                        bool wasRunning = DetectAndStopAcquisition(scpi, vendorUpper);
                        byte[] rawImage = TryFetchScreenshot(scpi, vendorUpper);

                        if (rawImage == null || rawImage.Length < 32)
                        {
                            Log("All screenshot command attempts failed.", LogLevel.Error);
                            failMode = true;
                            Beep("error");
                            if (wasRunning) TryRun(scpi);
                            return;
                        }

                        bool isPng = rawImage.Length >= 8 &&
                                     rawImage[0] == 0x89 && rawImage[1] == 0x50 &&
                                     rawImage[2] == 0x4E && rawImage[3] == 0x47;
                        bool isBmp = rawImage.Length >= 2 && rawImage[0] == 0x42 && rawImage[1] == 0x4D;

                        if (!isPng && !isBmp)
                        {
                            Log("Unknown image format signature.", LogLevel.Error);
                            failMode = true;
                            if (wasRunning) TryRun(scpi);
                            return;
                        }

                        Log($"Screenshot format: {(isPng ? "PNG" : "BMP")}, bytes={rawImage.Length}", LogLevel.Info);

                        if (!Directory.Exists(outputFolder))
                            Directory.CreateDirectory(outputFolder);

                        if (isBmp)
                        {
                            int width, height, bpp; string validationError;
                            if (!ValidateScopeBmp(rawImage, out width, out height, out bpp, out validationError, true))
                            {
                                Log("BMP validation failed: " + validationError, LogLevel.Error);
                                failMode = true;
                                Beep("error");
                                if (wasRunning) TryRun(scpi);
                                return;
                            }
                        }

                        using (var ms = new MemoryStream(rawImage))
                        using (var img = Image.FromStream(ms, false, false))
                        {
                            bool existed = File.Exists(outputFileName);
                            img.Save(outputFileName, ImageFormat.Png);
                            Log((existed ? "Overwrote " : "Saved ") + Path.GetFullPath(outputFileName), LogLevel.Notice);
                            SetPictureBoxImage((Image)img.Clone());
                        }

                        pictureBoxBorderColor = Color.Green;
                        success = true;
                        failMode = false;
                        Beep("after");

                        if (wasRunning) TryRun(scpi);

                        for (int i = 0; i < 4; i++)
                        {
                            var line = scpi.TryQuery(":SYST:ERR?", timeoutMs: 800);
                            if (string.IsNullOrWhiteSpace(line)) break;
                            if (line.StartsWith("0,", StringComparison.Ordinal)) break;
                            Log("Instrument error: " + line.Trim(), LogLevel.Warning);
                        }
                    }
                }
                catch (TimeoutException tex)
                {
                    Log("Capture failed (timeout): " + tex.Message, LogLevel.Error);
                }
                catch (IOException ioex)
                {
                    Log("Capture failed (I/O): " + ioex.Message, LogLevel.Error);
                }
                catch (InvalidDataException idex)
                {
                    Log("Capture failed (invalid data): " + idex.Message, LogLevel.Error);
                }
                catch (Exception ex)
                {
                    Log("Capture failed: " + ex.Message, LogLevel.Error);
                }
                finally
                {
                    if (!success) ClearPictureBoxImage();
                }
            });
        }

        private bool DetectAndStopAcquisition(RawScpiClient scpi, string vendorUpper)
        {
            bool running = true;
            string stat = scpi.TryQuery(":TRIG:STAT?", timeoutMs: 1200);
            if (string.IsNullOrWhiteSpace(stat)) stat = scpi.TryQuery(":TRIG:STATE?", timeoutMs: 1200);
            if (string.IsNullOrWhiteSpace(stat)) stat = scpi.TryQuery(":TRIG:STATUS?", timeoutMs: 1200);

            if (!string.IsNullOrWhiteSpace(stat))
            {
                string s = stat.Trim().ToUpperInvariant();
                if (s.Contains("STOP") || s.Contains("HALT") || s.Contains("IDLE") || s.Contains("WAIT"))
                    running = false;
                Log("Trigger state: " + s, LogLevel.Info);
            }
            else
            {
                Log("No trigger state response; assuming running.", LogLevel.Warning);
            }

            if (running)
            {
                if (!TryWrite(scpi, ":STOP")) TryWrite(scpi, ":ACQ:STATE 0");
                try { scpi.WaitOpc(1500); } catch { }
                System.Threading.Thread.Sleep(120);
                Log("Scope acquisition stopped for screenshot.", LogLevel.Info);
            }
            else
            {
                Log("Scope acquisition already stopped.", LogLevel.Info);
            }
            return running;
        }

        private byte[] TryFetchScreenshot(RawScpiClient scpi, string vendorUpper)
        {
            string[] attempts =
            {
                ":DISP:DATA?",
                ":DISP:DATA? PNG",
                ":DISP:DATA? ON,0,PNG",
                ":HARDcopy:DATA? PNG",
                ":HCOPy:DATA? PNG",
                ":DISP:DATA? ON,0,BMP",
                ":SCDP?"
            };
            foreach (var cmd in attempts)
            {
                Log("Trying screenshot command: " + cmd, LogLevel.Info);
                byte[] data = scpi.TryQueryBinaryBlock(cmd, 50 * 1024 * 1024, 8000);
                if (data != null && data.Length > 64)
                    return data;
            }
            return null;
        }

        private bool TryWrite(RawScpiClient scpi, string cmd)
        {
            try { scpi.WriteLine(cmd); return true; } catch { return false; }
        }

        private void TryRun(RawScpiClient scpi)
        {
            try
            {
                if (!TryWrite(scpi, ":RUN")) TryWrite(scpi, ":ACQ:STATE 1");
                scpi.WaitOpc(1500);
                Log("Acquisition resumed.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log("Failed to resume scope acquisition: " + ex.Message, LogLevel.Warning);
            }
        }

        // ================= UI HELPERS / DRAWING =================
        private void textBoxPinRange_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (!hotkeyMode && buttonCaptureContinuelsy != null && buttonCaptureContinuelsy.Enabled)
                    buttonCaptureContinuelsy.PerformClick();
            }
        }

        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (!captureInProgress && buttonCaptureOnce != null && buttonCaptureOnce.Enabled)
                    buttonCaptureOnce.PerformClick();
            }
        }

        private void PictureBox1_Paint(object sender, PaintEventArgs e)
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

        // ================= NETWORK HELPERS =================
        private void SuggestNextSteps()
        {
            Log($"Verify scope IP [{scopeIp}] and TCP port [{scopePort}] are correct and reachable.", LogLevel.Error);
            Beep("error");
            failMode = true;
        }

        private bool TestTcpPort(string host, int port, int timeoutMs)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var ar = client.BeginConnect(host, port, null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(timeoutMs);
                    if (!ok) return false;
                    client.EndConnect(ar);
                    return true;
                }
            }
            catch { return false; }
        }

        // ================= BMP VALIDATION =================
        private bool ValidateScopeBmp(byte[] data,
                                      out int width,
                                      out int height,
                                      out int bitsPerPixel,
                                      out string reason,
                                      bool patchHeader)
        {
            width = 0; height = 0; bitsPerPixel = 0; reason = "";
            if (data == null || data.Length < 54) { reason = "Too small for BMP header."; return false; }
            if (data[0] != 0x42 || data[1] != 0x4D) { reason = "Missing BM signature."; return false; }
            uint headerFileSize = ReadUInt32LE(data, 2);
            uint pixelDataOffset = ReadUInt32LE(data, 10);
            uint dibHeaderSize = ReadUInt32LE(data, 14);
            if (dibHeaderSize < 40) { reason = "Unsupported DIB header (<40)."; return false; }
            width = ReadInt32LE(data, 18);
            height = ReadInt32LE(data, 22);
            ushort planes = ReadUInt16LE(data, 26);
            bitsPerPixel = ReadUInt16LE(data, 28);
            Log($"Header: fileSizeField={headerFileSize}, pixelOffset={pixelDataOffset}, dibSize={dibHeaderSize}, w={width}, h={height}, bpp={bitsPerPixel}", LogLevel.Info);
            if (planes != 1) { reason = "Planes != 1."; return false; }
            if (bitsPerPixel != 24 && bitsPerPixel != 16 && bitsPerPixel != 32) { reason = "Unsupported bpp " + bitsPerPixel; return false; }
            if (width <= 0 || Math.Abs(height) <= 0 || width > 10000 || Math.Abs(height) > 10000) { reason = "Unreasonable dimensions " + width + "x" + height; return false; }
            int rowBytesRaw = (width * bitsPerPixel + 7) / 8;
            int stride = (rowBytesRaw + 3) & ~3;
            long needed = pixelDataOffset + (long)stride * Math.Abs(height);
            if (needed > data.Length) { reason = "Pixel data truncated. Need " + needed + ", have " + data.Length; return false; }
            if (headerFileSize != data.Length)
            {
                if (headerFileSize == 0 || headerFileSize + pixelDataOffset == data.Length)
                {
                    Log("Notice: Non-standard [FileSize] field; patching.", LogLevel.Notice);
                    if (patchHeader) PatchFileSize(data, (uint)data.Length);
                }
                else if (Math.Abs((long)headerFileSize - data.Length) <= 8)
                {
                    Log("Minor [FileSize] mismatch tolerated.", LogLevel.Warning);
                }
                else
                {
                    reason = $"File size mismatch. Header={headerFileSize}, Actual={data.Length}";
                    return false;
                }
            }
            bool anyNonZero = false;
            int pixelStart = (int)pixelDataOffset;
            int pixelEnd = data.Length;
            int sampleStep = Math.Max(1, stride / 32);
            for (int i = pixelStart; i < pixelEnd; i += sampleStep)
            {
                if (data[i] != 0) { anyNonZero = true; break; }
            }
            if (!anyNonZero) Log("Warning: Capture appears black; continuing.", LogLevel.Warning);
            return true;
        }

        private void PatchFileSize(byte[] data, uint actual)
        {
            data[2] = (byte)(actual & 0xFF);
            data[3] = (byte)((actual >> 8) & 0xFF);
            data[4] = (byte)((actual >> 16) & 0xFF);
            data[5] = (byte)((actual >> 24) & 0xFF);
        }

        // ================= LOGGING =================
        private void Log(string message) => Log(message, InferLevel(message));
        private LogLevel InferLevel(string msg)
        {
            if (msg.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                msg.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0) return LogLevel.Error;
            if (msg.StartsWith("Warning", StringComparison.OrdinalIgnoreCase)) return LogLevel.Warning;
            if (msg.StartsWith("Notice", StringComparison.OrdinalIgnoreCase)) return LogLevel.Notice;
            return LogLevel.Info;
        }
        private void Log(string message, LogLevel level)
        {
            if (richTextBoxLog == null) return;
            if (richTextBoxLog.InvokeRequired)
                richTextBoxLog.BeginInvoke(new Action(() => AppendColoredLine(message, level)));
            else
                AppendColoredLine(message, level);
        }
        private void AppendColoredLine(string message, LogLevel level)
        {
            if (richTextBoxLog == null) return;
            Color color;
            switch (level)
            {
                case LogLevel.Warning: color = Color.Khaki; break;
                case LogLevel.Error: color = Color.OrangeRed; break;
                case LogLevel.Notice: color = Color.DeepSkyBlue; break;
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

        // ================= HELPERS =================
        private static uint ReadUInt32LE(byte[] d, int o) => (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));
        private static int ReadInt32LE(byte[] d, int o) => d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24);
        private static ushort ReadUInt16LE(byte[] d, int o) => (ushort)(d[o] | (d[o + 1] << 8));

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

        private void button1_Click_1(object sender, EventArgs e)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(exeDir)) return;
            string outDir = Path.Combine(exeDir, outputFolder);
            if (!Directory.Exists(outDir))
            {
                try { Directory.CreateDirectory(outDir); }
                catch (Exception ex) { Log("Could not create output folder: " + ex.Message, LogLevel.Error); return; }
            }
            try { Process.Start("explorer.exe", "\"" + outDir + "\""); }
            catch (Exception ex) { Log("Failed to open output folder: " + ex.Message, LogLevel.Error); }
        }
    }

    internal sealed class RawScpiClient : IDisposable
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly byte[] _one = new byte[1];

        private RawScpiClient(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
        }

        public static RawScpiClient Connect(string host, int port, int connectTimeoutMs, int ioTimeoutMs)
        {
            var client = new TcpClient();
            var ar = client.BeginConnect(host, port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(connectTimeoutMs))
            {
                try { client.Close(); } catch { }
                throw new TimeoutException("SCPI connect timeout.");
            }
            client.EndConnect(ar);
            client.NoDelay = true;
            client.SendTimeout = ioTimeoutMs;
            client.ReceiveTimeout = ioTimeoutMs;
            return new RawScpiClient(client);
        }

        public void WriteLine(string command)
        {
            if (command == null) command = "";
            var data = Encoding.ASCII.GetBytes(command + "\n");
            _stream.Write(data, 0, data.Length);
            _stream.Flush();
        }

        public string QueryLine(string command, int maxLen = 4096, bool trim = true)
        {
            WriteLine(command);
            string s = ReadLine(maxLen);
            return trim && s != null ? s.Trim() : s;
        }

        public string TryQuery(string command, int maxLen = 4096, bool trim = true, int timeoutMs = 1500)
        {
            int old = _client.ReceiveTimeout;
            try
            {
                _client.ReceiveTimeout = timeoutMs;
                return QueryLine(command, maxLen, trim);
            }
            catch { return null; }
            finally { _client.ReceiveTimeout = old; }
        }

        public byte[] TryQueryBinaryBlock(string command, int maxBytes, int timeoutMs)
        {
            int old = _client.ReceiveTimeout;
            try
            {
                _client.ReceiveTimeout = timeoutMs;
                WriteLine(command);
                var data = ReadIeee4882Block(maxBytes);
                ConsumeCrLf();
                return data;
            }
            catch { return null; }
            finally { _client.ReceiveTimeout = old; }
        }

        public void ClearStatus() { WriteLine("*CLS"); }

        public void WaitOpc(int timeoutMs)
        {
            int old = _client.ReceiveTimeout;
            try
            {
                _client.ReceiveTimeout = timeoutMs;
                QueryLine("*OPC?");
            }
            finally { _client.ReceiveTimeout = old; }
        }

        public void DrainInput(int maxBytes = 8192)
        {
            try
            {
                var sock = _client.Client;
                var buf = new byte[256];
                int total = 0;
                while (sock.Available > 0 && total < maxBytes)
                {
                    int toRead = Math.Max(1, Math.Min(buf.Length, sock.Available));
                    int n = _stream.Read(buf, 0, toRead);
                    if (n <= 0) break;
                    total += n;
                }
            }
            catch { }
        }

        public string ReadLine(int maxLen)
        {
            var sb = new StringBuilder(128);
            int b;
            while ((b = ReadByte()) >= 0)
            {
                if (b == '\n') break;
                if (b != '\r') sb.Append((char)b);
                if (sb.Length >= maxLen) break;
            }
            if (b < 0 && sb.Length == 0) return null;
            return sb.ToString();
        }

        private int ReadByte()
        {
            int n = _stream.Read(_one, 0, 1);
            if (n <= 0) return -1;
            return _one[0];
        }

        private int ReadByteSkipWhitespace()
        {
            int b;
            do { b = ReadByte(); } while (b == ' ' || b == '\t' || b == '\r' || b == '\n');
            return b;
        }

        public byte[] ReadIeee4882Block(int maxBytes)
        {
            int hash = ReadByteSkipWhitespace();
            if (hash != '#') throw new InvalidDataException("Invalid IEEE488.2 block (missing '#').");
            int ndChar = ReadByte();
            if (ndChar < '0' || ndChar > '9') throw new InvalidDataException("Invalid IEEE488.2 block (Nd not digit).");
            int nd = ndChar - '0';
            if (nd == 0) throw new NotSupportedException("Indefinite-length blocks (Nd=0) not supported.");
            int length = 0;
            for (int i = 0; i < nd; i++)
            {
                int d = ReadByte();
                if (d < '0' || d > '9') throw new InvalidDataException("Invalid block length digit.");
                checked { length = length * 10 + (d - '0'); }
            }
            if (length < 0 || length > maxBytes)
                throw new InvalidDataException("Block length " + length + " exceeds limit " + maxBytes + ".");
            var data = new byte[length];
            ReadExactly(data, 0, length);
            return data;
        }

        public void ConsumeCrLf()
        {
            var sock = _client.Client;
            var buf = new byte[1];
            while (sock.Available > 0)
            {
                int peeked = sock.Receive(buf, 0, 1, SocketFlags.Peek);
                if (peeked <= 0) break;
                if (buf[0] == (byte)'\r' || buf[0] == (byte)'\n')
                {
                    _stream.Read(buf, 0, 1);
                }
                else break;
            }
        }

        private void ReadExactly(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int n = _stream.Read(buffer, offset + read, count - read);
                if (n <= 0) throw new IOException("Connection closed while reading.");
                read += n;
            }
        }

        public void Dispose()
        {
            try { _stream.Dispose(); } catch { }
            try { _client.Close(); } catch { }
        }
    }
}