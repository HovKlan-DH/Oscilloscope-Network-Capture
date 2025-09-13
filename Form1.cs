using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
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
        private string oncPage = "https://commodore-repair-toolbox.dk";
        private string oncPageAutoUpdate = "/auto-update/";
        private bool beepEnabled = true;
       
        private string versionThis = "";
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

        private enum ScopeVendor { Unknown, Rigol, Siglent }
        private ScopeVendor detectedVendor = ScopeVendor.Unknown;
        private string lastIdn = null;

        // ###########################################################################################
        // Form constructor
        // ###########################################################################################

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
            versionThis = date + rev;
            labelProductVersion.Text = "Version " + versionThis;

            if (textBoxIp != null) textBoxIp.Text = scopeIp;

            if (comboBoxRegion != null)
            {
                comboBoxRegion.SelectedIndexChanged += comboBoxRegion_SelectedIndexChanged;
                if (comboBoxRegion.Items.Count > 0)
                    comboBoxRegion.SelectedIndex = 0;
            }

            checkBoxBeep.CheckedChanged += checkBoxBeep_CheckedChanged;

            LoadConfig();
            EnsureLog();

            string helpTxt = @"{\rtf1\ansi {\fs28{\b Rigol}}\line ";
            helpTxt += @"Typical port is {\b 5555} (I am actually not sure on this - is this typical?).\line ";
            helpTxt += @"Confirmed working on:\line ";
            helpTxt += @"  * Rigol DS2202A\line ";
            helpTxt += @"\line ";
            helpTxt += @"{\fs28{\b Siglent}}\line ";
            helpTxt += @"Typical port is {\b 5025} (I am actually not sure on this - is this typical?).\line ";
            helpTxt += @"Confirmed working on:\line ";
            helpTxt += @"    * Siglent SDS 1204X - E \line";
            helpTxt += @"\line";
            helpTxt += @"{\fs28{\b Variables to use in filename format}}\line ";
            helpTxt += @"    * \{Region\}\line ";
            helpTxt += @"    * \{Component\}\line ";
            helpTxt += @"    * \{Pin\}\line ";
            helpTxt += @"    * \{Date\} is YYYYMMDD - e.g. 20251231\line ";
            helpTxt += @"    * \{Time\} is HHMMSS - e.g. 235959\line ";
            helpTxt += @"\line";
            helpTxt += @"{\fs28{\b Troubleshoot no connectivity to you oscilloscope}}\line ";
            helpTxt += @"If you do not get any connection to your oscilloscope, then please do validate that your computer can actually connect to the oscilloscope over network. You can do this by this simple commandline prompt, but it does require that you do have the ""telnet"" command installed (can be installed from ""Programs and Feaures > Turn Windows features on or off"" from Windows Control Panel):\line\line ";
            helpTxt += @"    {\b telnet 192.168.0.100 5555}\line\line ";
            helpTxt += @"If this results in a black screen, then you do have connectivity. You of course needs to adapt this for your scope, so it will have another IP address and probably also another port instead of ""5555"". Maybe also your scope has a web interface, so you can try also accessing it on its IP addresses for both HTTP and HTTPS.\line ";
            richTextBoxHelp.Rtf = helpTxt;

            textBoxComponent.Text = component;
            textBoxFilenameFormat.Text = filenameFormat;

            textBoxPort.Text = scopePort.ToString();
            textBoxPort.Leave += textBoxPort_Leave;
            textBoxPort.KeyDown += textBoxPort_KeyDown;
            textBoxFilenameFormat.Leave += textBoxFilenameFormat_Leave;
            textBoxFilenameFormat.KeyDown += textBoxFilenameFormat_KeyDown;
            buttonCaptureOnce.Click += buttonCaptureOnce_Click;
            buttonCaptureContinuelsy.Click += buttonCaptureContinuelsy_Click;
            buttonCheckScope.Click += buttonCheckScope_Click;
            richTextBoxAbout.LinkClicked += richTextBoxAbout_LinkClicked;

            this.KeyPreview = true;
            this.KeyDown += Form_KeyDown;

            pictureBoxImage.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxImage.BorderStyle = BorderStyle.None;
            pictureBoxImage.Paint += PictureBoxImage_Paint;
            pictureBoxIcon.SizeMode = PictureBoxSizeMode.Zoom;
            textBoxIp.Leave += textBoxIp_Leave;
            textBoxIp.KeyDown += textBoxIp_KeyDown;
            textBoxCapturePin.KeyDown += textBoxCapturePin_KeyDown;
            textBoxCapturePinStart.KeyDown += textBoxPinRange_KeyDown;
            textBoxCapturePinEnd.KeyDown += textBoxPinRange_KeyDown;

            checkBoxBeep.Checked = beepEnabled;

            Log("Ready.", LogLevel.Info);
            richTextBoxAction.Text = "Ready for capture";
            initializing = false;

            GetOnlineVersion();
        }

        // ###########################################################################################
        // Hyperlink click handler for the "About" tab
        // ###########################################################################################

        private void richTextBoxAbout_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                string url = e.LinkText?.Trim();
                if (string.IsNullOrEmpty(url)) return;

                // Normalize (optional)
                if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url.TrimStart('/');
                }

                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
                Log("Opened URL: " + url, LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log("Failed to open URL: " + ex.Message, LogLevel.Error);
            }
        }

        // ###########################################################################################
        // Filename format textbox handlers
        // ###########################################################################################

        private void textBoxFilenameFormat_Leave(object sender, EventArgs e) => ApplyFilenameFormatFromTextBox();

        private void textBoxFilenameFormat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyFilenameFormatFromTextBox();
                e.SuppressKeyPress = true;
            }
        }
        
        // ###########################################################################################
        // Port textbox handlers
        // ###########################################################################################

        private void textBoxPort_Leave(object sender, EventArgs e) => ApplyPortFromTextBox();

        private void textBoxPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyPortFromTextBox();
                e.SuppressKeyPress = true;
            }
        }

        // ###########################################################################################
        // Load configuration file
        // ###########################################################################################

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

        // ###########################################################################################
        // Save configuration file
        // ###########################################################################################

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

        private void comboBoxRegion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            SaveConfig();
        }

        // ###########################################################################################
        // IP textbox handlers
        // ###########################################################################################

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

        // ###########################################################################################
        // Check scope connectivity
        // ###########################################################################################

        private async void buttonCheckScope_Click(object sender, EventArgs e)
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
                            lastIdn = idn;
                            Log("IDN: " + (idn ?? "").Trim(), LogLevel.Info);
                            detectedVendor = DetermineVendor(idn);
                            if (detectedVendor != ScopeVendor.Unknown)
                                Log("Detected vendor: " + detectedVendor, LogLevel.Notice);

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
                        Beep("error");
                    }
                    catch (Exception ex)
                    {
                        Log("Connectivity check failed: " + ex.Message, LogLevel.Error);
                        ClearPictureBoxImage();
                        Beep("error");
                    }
                });
            }
            finally
            {
                if (buttonCheckScope != null) buttonCheckScope.Enabled = true;
            }
        }

        private ScopeVendor DetermineVendor(string idn)
        {
            if (string.IsNullOrWhiteSpace(idn)) return ScopeVendor.Unknown;
            var up = idn.ToUpperInvariant();
            if (up.Contains("SIGLENT")) return ScopeVendor.Siglent;
            if (up.Contains("RIGOL")) return ScopeVendor.Rigol;
            return ScopeVendor.Unknown;
        }

        // ###########################################################################################
        // Update "Action" textbox
        // ###########################################################################################

        private void UpdatePinStatusText()
        {
            if (richTextBoxAction == null) return;
            if (pinStart <= pinEnd)
                richTextBoxAction.Text = $"Ready to capture pin {pinStart} of {pinEnd}\r\nPress [ENTER] to capture, [ESC] to stop.";
            else
                richTextBoxAction.Text = "Capture completed";
        }

        // ###########################################################################################
        // Hotkey mode
        // ###########################################################################################

        private void buttonCaptureContinuelsy_Click(object sender, EventArgs e)
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

        private async void Form_KeyDown(object sender, KeyEventArgs e)
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

        // ###########################################################################################
        // Initialize and update "pinStart" and "pinEnd" in the continuesly capture
        // ###########################################################################################

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

        // ###########################################################################################
        // Start single capture
        // ###########################################################################################

        private async void buttonCaptureOnce_Click(object sender, EventArgs e)
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

        // ###########################################################################################
        // Build capture filename from format and inputs
        // ###########################################################################################

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

            // New date/time values
            string dateVal = DateTime.Now.ToString("yyyyMMdd");
            string timeVal = DateTime.Now.ToString("HHmmss");

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "component", componentVal },
                { "pin", pinVal },
                { "region", regionVal },
                { "date", dateVal },
                { "time", timeVal }
            };

            string expanded = Regex.Replace(
                format,
                @"\{(component|pin|region|date|time)\}",
                m =>
                {
                    string key = m.Groups[1].Value;
                    string val;
                    return map.TryGetValue(key, out val) ? val : "";
                },
                RegexOptions.IgnoreCase);

            if (string.IsNullOrWhiteSpace(expanded))
                expanded = "capture_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

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

        // ###########################################################################################
        // Sanitize string for use in file names
        // ###########################################################################################

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

        // ###########################################################################################
        // Capture screen to file (main logic)
        // ###########################################################################################

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
                    using (var scpi = RawScpiClient.Connect(host, port, connectTimeoutMs, ioTimeoutMs))
                    {
                        PrepareSession(scpi);

                        bool wasRunning = StopAcquisitionIfRunning(scpi);

                        byte[] rawImage = FetchImage(scpi);
                        if (rawImage == null || rawImage.Length < 32)
                        {
                            Fail("All screenshot command attempts failed.", wasRunning, scpi);
                            return;
                        }

                        ImageKind kind = DetectImageKind(rawImage);
                        if (kind == ImageKind.Unknown)
                        {
                            Fail("Unknown image format signature.", wasRunning, scpi);
                            return;
                        }

                        if (kind == ImageKind.Bmp && !ValidateBmp(rawImage))
                        {
                            Fail("BMP validation failed (see log).", wasRunning, scpi);
                            return;
                        }

                        if (!Directory.Exists(outputFolder))
                            Directory.CreateDirectory(outputFolder);

                        SaveAndDisplay(rawImage, outputFileName);

                        pictureBoxBorderColor = Color.Green;
                        success = true;
                        failMode = false;
                        Beep("after");

                        if (wasRunning) ResumeAcquisition(scpi);
                        DrainInstrumentErrors(scpi);
                    }
                }
                catch (TimeoutException tex)
                {
                    Log("Capture failed (timeout): " + tex.Message, LogLevel.Error);
                    Beep("error");
                }
                catch (IOException ioex)
                {
                    Log("Capture failed (I/O): " + ioex.Message, LogLevel.Error);
                    Beep("error");
                }
                catch (InvalidDataException idex)
                {
                    Log("Capture failed (invalid data): " + idex.Message, LogLevel.Error);
                    Beep("error");
                }
                catch (Exception ex)
                {
                    Log("Capture failed: " + ex.Message, LogLevel.Error);
                    Beep("error");
                }
                finally
                {
                    if (!success) ClearPictureBoxImage();
                }
            });
        }

        private void PrepareSession(RawScpiClient scpi)
        {
            Log($"Connecting {scopeIp}:{scopePort}", LogLevel.Info);
            scpi.DrainInput();
            scpi.ClearStatus();
            string idn = scpi.TryQuery("*IDN?", timeoutMs: 2500);
            if (!string.IsNullOrWhiteSpace(idn))
            {
                lastIdn = idn;
                detectedVendor = DetermineVendor(idn);
                Log("IDN: " + idn.Trim(), LogLevel.Info);
                Log("Vendor classification: " + detectedVendor, LogLevel.Notice);
            }
        }

        private bool StopAcquisitionIfRunning(RawScpiClient scpi)
        {
            string vendorUpper = "";
            if (!string.IsNullOrWhiteSpace(lastIdn))
                vendorUpper = lastIdn.Split(',')[0].Trim().ToUpperInvariant();
            return DetectAndStopAcquisition(scpi, vendorUpper);
        }

        private byte[] FetchImage(RawScpiClient scpi) => FetchScreenshot(scpi, detectedVendor);

        private enum ImageKind { Unknown, Png, Bmp }

        private ImageKind DetectImageKind(byte[] raw)
        {
            if (raw.Length >= 8 &&
                raw[0] == 0x89 && raw[1] == 0x50 &&
                raw[2] == 0x4E && raw[3] == 0x47)
                return ImageKind.Png;
            if (raw.Length >= 2 && raw[0] == 0x42 && raw[1] == 0x4D)
                return ImageKind.Bmp;
            return ImageKind.Unknown;
        }

        private bool ValidateBmp(byte[] raw)
        {
            int width, height, bpp;
            string validationError;
            if (!ValidateScopeBmp(raw, out width, out height, out bpp, out validationError, true))
            {
                Log("BMP validation failed: " + validationError, LogLevel.Error);
                failMode = true;
                Beep("error");
                return false;
            }
            return true;
        }

        private void SaveAndDisplay(byte[] rawImage, string outputFileName)
        {
            using (var ms = new MemoryStream(rawImage))
            using (var img = Image.FromStream(ms, false, false))
            {
                bool existed = File.Exists(outputFileName);
                img.Save(outputFileName, ImageFormat.Png);
                Log((existed ? "Overwrote " : "Saved ") + Path.GetFullPath(outputFileName), LogLevel.Notice);
                SetPictureBoxImage((Image)img.Clone());
            }
        }

        private void ResumeAcquisition(RawScpiClient scpi) => TryRun(scpi);

        private void DrainInstrumentErrors(RawScpiClient scpi)
        {
            for (int i = 0; i < 4; i++)
            {
                var line = scpi.TryQuery(":SYST:ERR?", timeoutMs: 800);
                if (string.IsNullOrWhiteSpace(line)) break;
                if (line.StartsWith("0,", StringComparison.Ordinal)) break;
                Log("Instrument error: " + line.Trim(), LogLevel.Warning);
            }
        }

        private void Fail(string message, bool wasRunning, RawScpiClient scpi)
        {
            Log(message, LogLevel.Error);
            failMode = true;
            Beep("error");
            if (wasRunning) ResumeAcquisition(scpi);
        }

        // ###########################################################################################
        // Fetch screenshot using vendor-specific (Siglent for now) or generic (Rigol) commands
        // ###########################################################################################

        private byte[] FetchScreenshot(RawScpiClient scpi, ScopeVendor vendor)
        {
            if (vendor == ScopeVendor.Siglent)
            {
                var img = TryFetchSiglent(scpi);
                if (img != null) return img;
                Log("Siglent path failed; falling back to generic list.", LogLevel.Warning);
            }
            // Rigol or fallback generic (existing logic)
            return TryFetchRigolStyle(scpi);
        }

        // ###########################################################################################
        // Rigol - try various commands to fetch screenshot in different formats
        // ###########################################################################################

        private byte[] TryFetchRigolStyle(RawScpiClient scpi)
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
                Log("Trying screenshot command (Rigol path): " + cmd, LogLevel.Info);
                byte[] data = scpi.TryQueryBinaryBlock(cmd, 50 * 1024 * 1024, 8000);
                if (data != null && data.Length > 64)
                {
                    Log("Command " + cmd + " succeeded (block, bytes=" + data.Length + ", head=" + HexPreview(data, 16) + ")", LogLevel.Info);
                    return data;
                }
            }
            return null;
        }

        // ###########################################################################################
        // Siglent - try various commands to fetch screenshot in different formats
        // ###########################################################################################

        private byte[] TryFetchSiglent(RawScpiClient scpi)
        {
            string[] siglent = { "SCDP", "SCDP?", ":SCDP?" };
            foreach (var cmd in siglent)
            {
                Log("Trying Siglent screenshot command: " + cmd, LogLevel.Info);
                string mode;
                byte[] data = scpi.TryQuerySiglentImage(cmd, 50 * 1024 * 1024, 12000, out mode);
                if (data != null && data.Length > 64)
                {
                    Log($"Siglent command {cmd} succeeded (mode={mode}, bytes={data.Length}, head={HexPreview(data, 16)})", LogLevel.Info);
                    return data;
                }
                Log($"Siglent command {cmd} produced no usable data.", LogLevel.Warning);
            }
            return null;
        }


        // ###########################################################################################
        // Detect if scope acquisition is running and stop it if so
        // ###########################################################################################

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

        // ###########################################################################################
        // Resume scope acquisition if we stopped it
        // ###########################################################################################

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

        // ###########################################################################################
        // Send a SCPI command and ignore errors
        // ###########################################################################################

        private bool TryWrite(RawScpiClient scpi, string cmd)
        {
            try { scpi.WriteLine(cmd); return true; } catch { return false; }
        }

        // ###########################################################################################
        // Pressing ENTER should trigger capture in both places (once or continuesly)

        private void textBoxCapturePin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (!captureInProgress && buttonCaptureOnce != null && buttonCaptureOnce.Enabled)
                    buttonCaptureOnce.PerformClick();
            }
        }

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

        // ###########################################################################################
        // Paint event for scope image box - draws border
        // ###########################################################################################

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

        // ###########################################################################################
        // Set picture box image thread-safely
        // ###########################################################################################

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

        // ###########################################################################################
        // Clear picture box - e.g. if a fail happens
        // ###########################################################################################

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

        // ###########################################################################################
        // Beep sounds
        // ###########################################################################################

        private void checkBoxBeep_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxBeep == null) return;
            beepEnabled = checkBoxBeep.Checked;
            if (initializing) return;
            SaveConfig();
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

        // ###########################################################################################
        // TCP port test (no ICMP)
        // ###########################################################################################

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

        // ###########################################################################################
        // BMP validation and patching
        // ###########################################################################################

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

        // ###########################################################################################
        // Logging with color coding
        // ###########################################################################################

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

        private static string HexPreview(byte[] data, int max)
        {
            if (data == null) return "";
            int n = Math.Min(max, data.Length);
            var sb = new StringBuilder(n * 3);
            for (int i = 0; i < n; i++)
            {
                sb.Append(data[i].ToString("X2"));
                if (i + 1 < n) sb.Append(' ');
            }
            return sb.ToString();
        }

        // ###########################################################################################
        // Open output capture folder in File Explorer
        // ###########################################################################################

        private void buttonOpenFolder_Click(object sender, EventArgs e)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(exeDir)) return;
            string outDir = Path.Combine(exeDir, outputFolder);
            if (!Directory.Exists(outDir))
            {
                try { Directory.CreateDirectory(outDir); }
                catch (Exception ex) { Log("Could not create output folder: " + ex.Message, LogLevel.Error); return; }
            }
            try
            {
                Process.Start("explorer.exe", "\"" + outDir + "\"");
            }
            catch (Exception ex)
            {
                Log("Failed to open output folder: " + ex.Message, LogLevel.Error);
                Beep("error");
            }
        }

        // ###########################################################################################
        // Check if there is a newer version online.
        // Test for now only, to see how stable this is.
        // ###########################################################################################

        private void GetOnlineVersion()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                    webClient.Headers.Add("user-agent", "ONC " + versionThis);

                    // Include some control POST data
                    var postData = new System.Collections.Specialized.NameValueCollection
                    {
                        { "control", "ONC" }
                    };

                    // Send the POST data to the server
                    byte[] responseBytes = webClient.UploadValues(oncPage + oncPageAutoUpdate, postData);

                    // Convert the "response bytes" to a human readable string
                    string onlineAvailableVersion = Encoding.UTF8.GetString(responseBytes);

                    if (onlineAvailableVersion.Substring(0, 7) == "Version")
                    {
                        onlineAvailableVersion = onlineAvailableVersion.Substring(9);
                        if (onlineAvailableVersion != versionThis)
                        {
                            /* do something (inform user) when a newer version is available online */
                        }
                    }
                }
            }
            catch 
            {
            }
        }

        private static uint ReadUInt32LE(byte[] d, int o) => (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));
        private static int ReadInt32LE(byte[] d, int o) => d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24);
        private static ushort ReadUInt16LE(byte[] d, int o) => (ushort)(d[o] | (d[o + 1] << 8));
    }


    // ###########################################################################################
    // Raw SCPI client with basic IEEE 488.2 block read support
    // ###########################################################################################

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

        public byte[] TryQuerySiglentImage(string command, int maxBytes, int timeoutMs, out string mode)
        {
            mode = "";
            int old = _client.ReceiveTimeout;
            try
            {
                _client.ReceiveTimeout = timeoutMs;
                WriteLine(command);

                int first = ReadByteSkipWhitespace();
                if (first < 0) return null;

                if (first == '#')
                {
                    mode = "block";
                    int ndChar = ReadByte();
                    if (ndChar < '0' || ndChar > '9') return null;
                    int nd = ndChar - '0';
                    if (nd == 0) return null;
                    int length = 0;
                    for (int i = 0; i < nd; i++)
                    {
                        int d = ReadByte();
                        if (d < '0' || d > '9') return null;
                        length = length * 10 + (d - '0');
                        if (length > maxBytes) return null;
                    }
                    var data = new byte[length];
                    ReadExactly(data, 0, length);
                    ConsumeCrLf();
                    return data;
                }

                // RAW path
                var header = new List<byte>();
                header.Add((byte)first);
                while (header.Count < 8)
                {
                    int b = ReadByte();
                    if (b < 0) break;
                    header.Add((byte)b);
                }

                if (header.Count >= 2 && header[0] == 0x42 && header[1] == 0x4D)
                {
                    mode = "raw-bmp";
                    while (header.Count < 54)
                    {
                        int b = ReadByte();
                        if (b < 0) break;
                        header.Add((byte)b);
                    }
                    if (header.Count < 54) return null;
                    uint fileSize = (uint)(header[2] | (header[3] << 8) | (header[4] << 16) | (header[5] << 24));
                    if (fileSize == 0 || fileSize > maxBytes) fileSize = (uint)Math.Min(maxBytes, 12 * 1024 * 1024);
                    var bmp = new byte[fileSize];
                    for (int i = 0; i < header.Count && i < bmp.Length; i++) bmp[i] = header[i];
                    if (fileSize > (uint)header.Count)
                        ReadExactly(bmp, header.Count, (int)fileSize - header.Count);
                    return bmp;
                }

                if (header.Count >= 8 &&
                    header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                {
                    mode = "raw-png";
                    var ms = new MemoryStream();
                    ms.Write(header.ToArray(), 0, header.Count);
                    while (ms.Length < maxBytes)
                    {
                        byte[] lenType = new byte[8];
                        ReadExactly(lenType, 0, 8);
                        ms.Write(lenType, 0, 8);
                        int chunkLen = (lenType[0] << 24) | (lenType[1] << 16) | (lenType[2] << 8) | lenType[3];
                        if (chunkLen < 0 || chunkLen > 32 * 1024 * 1024) break;
                        byte[] chunkPlusCrc = new byte[chunkLen + 4];
                        ReadExactly(chunkPlusCrc, 0, chunkPlusCrc.Length);
                        ms.Write(chunkPlusCrc, 0, chunkPlusCrc.Length);
                        string type = Encoding.ASCII.GetString(lenType, 4, 4);
                        if (type == "IEND") break;
                    }
                    return ms.ToArray();
                }

                mode = "raw-unknown";
                var raw = new List<byte>(header);
                var buf = new byte[4096];
                while (raw.Count < maxBytes)
                {
                    int n;
                    try { n = _stream.Read(buf, 0, buf.Length); }
                    catch { break; }
                    if (n <= 0) break;
                    for (int i = 0; i < n; i++) raw.Add(buf[i]);
                    if (n < buf.Length) break;
                }
                return raw.ToArray();
            }
            catch
            {
                return null;
            }
            finally
            {
                _client.ReceiveTimeout = old;
            }
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

    // ###########################################################################################
}