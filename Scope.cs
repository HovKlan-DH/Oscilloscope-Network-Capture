using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Oscilloscope_Network_Capture
{
    public sealed class Scope
    {
        public enum ScopeVendor { Unknown, Rigol, Siglent }

        private readonly Logger _logger;

        public Scope(Logger logger)
        {
            _logger = logger ?? new Logger();
        }

        // Returns true if the scope acquisition is currently running; false if stopped or on query failure.
        public Task<bool> IsAcquisitionRunningAsync(string host, int port, int connectTimeoutMs = 1500, int ioTimeoutMs = 3000)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var scpi = ScpiClient.Connect(host, port, connectTimeoutMs, ioTimeoutMs))
                    {
                        PrepareSession(scpi);
                        return QueryIsRunning(scpi, 800);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug("IsAcquisitionRunningAsync failed: " + ex.Message);
                    return false;
                }
            });
        }

        public ScopeVendor DetectedVendor { get; private set; } = ScopeVendor.Unknown;
        public string LastIdn { get; private set; }
        private string _lastSiglentTrigMode;

        public sealed class CaptureResult
        {
            public bool Success { get; set; }
            public string SavedPath { get; set; }
            public Image PreviewImage { get; set; } // Caller disposes
            public string Error { get; set; }
        }

        public async Task<bool> CheckConnectivityAsync(string host, int port, int connectTimeoutMs = 5000, int ioTimeoutMs = 60000)
        {
            bool success = false;

            _logger.Debug("Attempting TCP connectivity.");
            if (!TestTcpPort(host, port, 600))
                _logger.Warn($"Port {port} preliminary probe failed (will still attempt full connect).");
            else
                _logger.Debug($"Port {port} preliminary probe succeeded.");

            _logger.Info("Opening SCPI session: " + host + ":" + port);

            await Task.Run(() =>
            {
                try
                {
                    using (var scpi = ScpiClient.Connect(host, port, connectTimeoutMs, ioTimeoutMs))
                    {
                        scpi.DrainInput();
                        scpi.ClearStatus();

                        string idn = scpi.QueryLine("*IDN?");
                        LastIdn = idn;
                        _logger.Debug("IDN: " + (idn ?? "").Trim());
                        DetectedVendor = DetermineVendor(idn);

                        if (!string.IsNullOrWhiteSpace(idn))
                        {
                            var p = idn.Split(',');
                            if (p.Length >= 4)
                            {
                                _logger.Info("Connected with oscilloscope:");
                                _logger.Info("    Vendor: " + p[0]);
                                _logger.Info("    Model: " + p[1]);
                                _logger.Info("    Firmware: " + p[3]);
                                _logger.Notice("Oscilloscope is ready.");
                            }
                        }

                        for (int i = 0; i < 6; i++)
                        {
                            var line = scpi.TryQuery(":SYST:ERR?", timeoutMs: 1200);
                            if (string.IsNullOrWhiteSpace(line)) break;
                            if (line.StartsWith("0,", StringComparison.Ordinal)) break;
                            _logger.Warn("Instrument error: " + line.Trim());
                        }

                        success = true;
                    }
                }
                catch (TimeoutException tex)
                {
                    _logger.Error("Connectivity check failed (timeout): " + tex.Message);
                }
                catch (IOException ioex)
                {
                    _logger.Error("Connectivity check failed (I/O): " + ioex.Message);
                }
                catch (Exception ex)
                {
                    _logger.Error("Connectivity check failed: " + ex.Message);
                }
            });

            return success;
        }

        public Task AdjustTriggerLevelAsync(string host, int port, bool increase)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var scpi = ScpiClient.Connect(host, port, 5000, 5000))
                    {
                        PrepareSession(scpi);
                        var vendor = DetectedVendor;

                        // Resolve trigger source channel
                        string src = FirstNonEmpty(
                            () => QueryString(scpi, ":TRIGger:EDGE:SOURce?"),
                            () => QueryString(scpi, "TRIG:EDGE:SOUR?"),
                            () => QueryString(scpi, ":TRIG:SOUR?"));
                        if (string.IsNullOrWhiteSpace(src)) src = vendor == ScopeVendor.Siglent ? "C1" : "CHAN1";
                        src = src.Trim().ToUpperInvariant();

                        int chIdx = ParseChannelIndex(src);
                        string rigolChan = "CHAN" + chIdx;  // Rigol channel token
                        string siglentChan = "C" + chIdx;   // Siglent channel token

                        // Current trigger level (V)
                        double currentLevel = FirstDouble(
                            // Rigol channel-scoped first
                            () => QueryDouble(scpi, $":TRIG:LEV? {rigolChan}"),
                            () => QueryDouble(scpi, $":TRIGger:LEVel? {rigolChan}"),
                            // Generic
                            () => QueryDouble(scpi, ":TRIG:LEV?"),
                            // Rigol older variant
                            () => QueryDouble(scpi, $":TRIGger:LEVel {rigolChan}?"),
                            // Siglent guesses
                            () => QueryDouble(scpi, "TRIG_LEVEL?"),
                            () => QueryDouble(scpi, $"{siglentChan}:TRIG_LEVEL?")
                        ) ?? 0.0;

                        // Fixed step: always 0.25 V, align to quarter-volt grid regardless of volts/div
                        const double stepVolts = 0.25;
                        double grid = stepVolts;

                        // Snap logic:
                        // - If off-grid: snap toward requested direction (ceil for increase, floor for decrease).
                        // - If on-grid: move exactly one grid up/down.
                        double snappedNearest = RoundToIncrement(currentLevel, grid);
                        bool isAligned = NearlyEqual(currentLevel, snappedNearest, Math.Max(1e-9, grid * 1e-3));

                        double target = isAligned
                            ? currentLevel + (increase ? grid : -grid)
                            : (increase ? CeilToIncrement(currentLevel, grid) : FloorToIncrement(currentLevel, grid));

                        string targetStr = target.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);

                        bool sent =
                            // Rigol channel-scoped preferred
                            TryWrite(scpi, $":TRIG:LEV {rigolChan}, {targetStr}") ||
                            TryWrite(scpi, $":TRIGger:LEVel {rigolChan}, {targetStr}") ||
                            // Generic fallbacks
                            TryWrite(scpi, $":TRIG:LEV {targetStr}") ||
                            TryWrite(scpi, $"TRIG:EDGE:LEV {targetStr}") ||
                            // Siglent variants
                            TryWrite(scpi, $"TRIG_LEVEL {targetStr}") ||
                            TryWrite(scpi, $"{siglentChan}:TRIG_LEVEL {targetStr}");

                        if (!sent)
                        {
                            _logger.Warn("Trigger level command not accepted by scope.");
                            return;
                        }

                        try { scpi.WaitOpc(800); } catch { /* best-effort */ }
                        System.Threading.Thread.Sleep(50);

                        // Readback to verify
                        double after = FirstDouble(
                            () => QueryDouble(scpi, $":TRIG:LEV? {rigolChan}"),
                            () => QueryDouble(scpi, $":TRIGger:LEVel? {rigolChan}"),
                            () => QueryDouble(scpi, ":TRIG:LEV?"),
                            () => QueryDouble(scpi, "TRIG_LEVEL?"),
                            () => QueryDouble(scpi, $"{siglentChan}:TRIG_LEVEL?")
                        ) ?? double.NaN;

                        string prevStr = currentLevel.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                        if (double.IsNaN(after))
                        {
                            _logger.Notice($"Trigger level {(increase ? "attempted up" : "attempted down")} from {prevStr} V to {targetStr} V (readback unavailable).");
                        }
                        else
                        {
                            string afterStr = after.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                            if (Math.Abs(after - currentLevel) < (grid * 0.2))
                                _logger.Warn($"Trigger level unchanged ({afterStr} V). Check trigger source/channel on the scope.");
                            else
                                _logger.Notice($"Trigger level {(increase ? "increased" : "decreased")} from [{prevStr}V] to [{afterStr}V].");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("AdjustTriggerLevel failed: " + ex.Message);
                }
            });

            // ------- local helpers -------

            string QueryString(ScpiClient cli, string cmd)
            {
                if (!TryWrite(cli, cmd)) return null;
                var s = cli.ReadLine(256);
                return s != null ? s.Trim() : null;
            }

            double? QueryDouble(ScpiClient cli, string cmd)
            {
                var s = QueryString(cli, cmd);
                if (string.IsNullOrWhiteSpace(s)) return null;
                var d = ParseLastDouble(s);
                if (d.HasValue) return d;
                s = CleanupNumeric(s);
                double v;
                return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? (double?)v : null;
            }

            // Extract the last numeric token (handles replies like "CHAN1,0.01V")
            double? ParseLastDouble(string s)
            {
                int i = 0; double? last = null;
                while (i < s.Length)
                {
                    // start of a numeric?
                    if (char.IsDigit(s[i]) || s[i] == '.' || s[i] == '+' || s[i] == '-')
                    {
                        int j = i + 1;
                        bool seenDot = s[i] == '.';
                        while (j < s.Length)
                        {
                            char c = s[j];
                            if (char.IsDigit(c)) { j++; continue; }
                            if (c == '.' && !seenDot) { seenDot = true; j++; continue; }
                            if ((c == 'e' || c == 'E') && j + 1 < s.Length &&
    (char.IsDigit(s[j + 1]) || s[j + 1] == '+' || s[j + 1] == '-'))
                            {
                                j += 2;
                                while (j < s.Length && char.IsDigit(s[j])) j++;
                                continue;
                            }
                            break;
                        }
                        var token = s.Substring(i, j - i);
                        double v;
                        if (double.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                            last = v;
                        i = j;
                    }
                    else i++;
                }
                return last;
            }

            string CleanupNumeric(string s)
            {
                var sb = new System.Text.StringBuilder(s.Length);
                foreach (var c in s)
                {
                    if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E')
                        sb.Append(c);
                }
                return sb.ToString();
            }

            T FirstNonEmpty<T>(params System.Func<T>[] getters) where T : class
            {
                foreach (var g in getters)
                {
                    try
                    {
                        var v = g();
                        if (v is string str)
                        {
                            if (!string.IsNullOrWhiteSpace(str)) return v;
                        }
                        else if (v != null) return v;
                    }
                    catch { }
                }
                return null;
            }

            double? FirstDouble(params System.Func<double?>[] getters)
            {
                foreach (var g in getters)
                {
                    try
                    {
                        var v = g();
                        if (v.HasValue) return v;
                    }
                    catch { }
                }
                return null;
            }

            int ParseChannelIndex(string src)
            {
                // Accepts "CHAN1", "CH1", "C1" -> 1
                if (string.IsNullOrEmpty(src)) return 1;
                src = src.Trim().ToUpperInvariant();
                for (int i = 1; i <= 8; i++)
                {
                    if (src.Contains(i.ToString())) return i;
                }
                return 1;
            }

            double RoundToIncrement(double value, double inc)
            {
                if (inc <= 0) return value;
                return Math.Round(value / inc) * inc;
            }

            double FloorToIncrement(double value, double inc)
            {
                if (inc <= 0) return value;
                return Math.Floor(value / inc) * inc;
            }

            double CeilToIncrement(double value, double inc)
            {
                if (inc <= 0) return value;
                return Math.Ceiling(value / inc) * inc;
            }

            bool NearlyEqual(double a, double b, double eps)
            {
                return Math.Abs(a - b) <= eps;
            }
        }

        public async Task<CaptureResult> CaptureAsync(string host, int port, bool forceResume, string finalPath, int connectTimeoutMs = 5000, int ioTimeoutMs = 60000)
        {
            var result = new CaptureResult { Success = false, SavedPath = finalPath ?? "" };

            await Task.Run(() =>
            {
                try
                {
                    using (var scpi = ScpiClient.Connect(host, port, connectTimeoutMs, ioTimeoutMs))
                    {
                        PrepareSession(scpi);

                        bool wasRunning = StopAcquisitionIfRunning(scpi);

                        var rawImage = FetchScreenshot(scpi, DetectedVendor);
                        if (rawImage == null || rawImage.Length < 32)
                            throw new InvalidDataException("All screenshot command attempts failed.");

                        var kind = DetectImageKind(rawImage);
                        if (kind == ImageKind.Unknown)
                            throw new InvalidDataException("Unknown image format signature.");

                        if (kind == ImageKind.Bmp)
                        {
                            int w, h, bpp; string reason;
                            if (!ValidateBmp(rawImage, out w, out h, out bpp, out reason, true))
                                throw new InvalidDataException("BMP validation failed: " + reason);
                        }

                        var preview = SaveAndReturnPreview(rawImage, EnsureDirectory(finalPath));

                        bool forceRun = forceResume;
                        if (wasRunning || forceRun)
                        {
                            ForceAcquisition(scpi, forceRun && !wasRunning);
                        }
                        DrainInstrumentErrors(scpi);

                        result.Success = true;
                        result.PreviewImage = preview;
                    }
                }
                catch (Exception ex)
                {
                    result.Error = ex.Message;
                    _logger.Error("Capture failed: " + ex);
                }
            });

            return result;
        }

        public async Task AdjustTimebaseAsync(string host, int port, bool increase)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var scpi = ScpiClient.Connect(host, port, 4000, 4000))
                    {
                        scpi.DrainInput();
                        scpi.ClearStatus();

                        if (DetectedVendor == ScopeVendor.Unknown)
                        {
                            string idn = scpi.TryQuery("*IDN?", timeoutMs: 1500);
                            if (!string.IsNullOrWhiteSpace(idn))
                            {
                                LastIdn = idn;
                                DetectedVendor = DetermineVendor(idn);
                                _logger.Debug("IDN (timebase adjust): " + idn.Trim());
                                _logger.Debug("Vendor classification: " + DetectedVendor);
                            }
                        }

                        double current = QueryCurrentTimeDiv(scpi);
                        if (double.IsNaN(current) || current <= 0)
                        {
                            _logger.Error("Could not query current timebase scale.");
                            return;
                        }

                        int idx = FindNearestTimeDivIndex(current);
                        if (idx < 0) idx = 0;

                        int newIdx = idx + (increase ? 1 : -1);
                        newIdx = Math.Max(0, Math.Min(_timeDivSteps.Length - 1, newIdx));

                        double target = _timeDivSteps[newIdx];

                        if (SetTimeDiv(scpi, target))
                        {
                            _logger.Info("Timebase " + (increase ? "increased (zoom-out)" : "decreased (zoom-in)") +
                                         " from [" + FormatSeconds(current) + "] to [" + FormatSeconds(target) + "] per division.");
                        }
                        else
                        {
                            _logger.Error("Failed to set new timebase value.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Timebase adjustment failed: " + ex.Message);
                }
            });
        }

        public async Task SnapshotToggleAsync(string host, int port)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var scpi = ScpiClient.Connect(host, port, 4000, 4000))
                    {
                        scpi.DrainInput();
                        scpi.ClearStatus();

                        if (DetectedVendor == ScopeVendor.Unknown)
                        {
                            string idn = scpi.TryQuery("*IDN?", timeoutMs: 1200);
                            if (!string.IsNullOrWhiteSpace(idn))
                            {
                                LastIdn = idn;
                                DetectedVendor = DetermineVendor(idn);
                                _logger.Debug("IDN (snapshot): " + idn.Trim());
                            }
                        }

                        bool running = QueryIsRunning(scpi);

                        if (running)
                        {
                            if (StopAcq(scpi))
                                _logger.Notice("Acquisition stopped.");
                            else
                                _logger.Warn("Failed to stop acquisition.");
                        }
                        else
                        {
                            bool singleOk = TrySingleAcq(scpi);
                            if (!singleOk)
                            {
                                if (RunAcq(scpi))
                                {
                                    System.Threading.Thread.Sleep(180);
                                    StopAcq(scpi);
                                }
                            }

                            bool nowRunning = QueryIsRunning(scpi);
                            if (!nowRunning)
                                _logger.Notice("Refreshed single snapshot.");
                            else
                                _logger.Warn("Snapshot refresh uncertain (still running).");
                        }

                        var err = scpi.TryQuery(":SYST:ERR?", timeoutMs: 600);
                        if (!string.IsNullOrWhiteSpace(err) && !err.StartsWith("0,", StringComparison.Ordinal))
                            _logger.Warn("Instrument error (snapshot): " + err);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Snapshot toggle failed: " + ex.Message);
                }
            });
        }

        private bool TrySingleAcq(ScpiClient scpi)
        {
            bool ok;
            if (DetectedVendor == ScopeVendor.Siglent)
                ok = TryWrite(scpi, "SINGLE");
            else
                ok = TryWrite(scpi, ":SINGle") || TryWrite(scpi, ":RUN;:STOP");

            if (ok)
            {
                try { scpi.WaitOpc(3000); } catch { /* ignore */ }
            }
            return ok;
        }

        public async Task ResumeAcquisitionAsync(string host, int port)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var scpi = ScpiClient.Connect(host, port, 4000, 4000))
                    {
                        scpi.DrainInput();
                        scpi.ClearStatus();

                        if (DetectedVendor == ScopeVendor.Unknown)
                        {
                            string idn = scpi.TryQuery("*IDN?", timeoutMs: 1200);
                            if (!string.IsNullOrWhiteSpace(idn))
                            {
                                LastIdn = idn;
                                DetectedVendor = DetermineVendor(idn);
                                _logger.Debug("IDN (resume): " + idn.Trim());
                            }
                        }

                        bool sent = ForceRun(scpi);
                        bool after = WaitUntilRunning(scpi, timeoutMs: 2000, statusTimeoutMs: 200, pollDelayMs: 50);

                        if (sent && after) _logger.Notice("Acquisition resumed.");
                        else if (!after) _logger.Warn("Resume attempt failed (still stopped).");
                        else _logger.Warn("Resume command uncertain.");

                        var err = scpi.TryQuery(":SYST:ERR?", timeoutMs: 600);
                        if (!string.IsNullOrWhiteSpace(err) && !err.StartsWith("0,", StringComparison.Ordinal))
                            _logger.Warn("Instrument error (resume): " + err);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Resume acquisition failed: " + ex.Message);
                }
            });
        }

        // ---------------- internals ----------------

        private enum ImageKind { Unknown, Png, Bmp }

        private void PrepareSession(ScpiClient scpi)
        {
            _logger.Info("Connecting session");
            scpi.DrainInput();
            scpi.ClearStatus();
            string idn = scpi.TryQuery("*IDN?", timeoutMs: 2500);
            if (!string.IsNullOrWhiteSpace(idn))
            {
                LastIdn = idn;
                DetectedVendor = DetermineVendor(idn);
                _logger.Debug("IDN: " + idn.Trim());
                _logger.Debug("Vendor classification: " + DetectedVendor);
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

        private bool StopAcquisitionIfRunning(ScpiClient scpi)
        {
            if (DetectedVendor == ScopeVendor.Siglent)
            {
                _lastSiglentTrigMode = null;
                string mode = scpi.TryQuery("TRIG_MODE?", timeoutMs: 800);
                if (!string.IsNullOrWhiteSpace(mode))
                {
                    mode = mode.Trim().ToUpperInvariant();
                    _lastSiglentTrigMode = mode;
                    _logger.Debug("TRIG_MODE (Siglent): " + mode);
                }
                else
                {
                    _logger.Warn("TRIG_MODE? returned no data (Siglent); assuming running.");
                    mode = "AUTO";
                }

                bool running = mode == "AUTO" || mode == "NORM";
                if (running)
                {
                    if (TryWrite(scpi, "STOP"))
                    {
                        try { scpi.WaitOpc(1500); } catch { }
                        System.Threading.Thread.Sleep(120);
                        _logger.Debug("Acquisition stopped for screenshot (Siglent).");
                    }
                    else
                    {
                        _logger.Warn("Failed to send STOP (Siglent). Proceeding anyway.");
                    }
                }
                else
                {
                    _logger.Debug("Acquisition already not running (Siglent mode = " + mode + ").");
                }
                return running;
            }

            bool runningGeneric = true;
            string stat = scpi.TryQuery(":TRIG:STAT?", timeoutMs: 1200) ??
                          scpi.TryQuery(":TRIG:STATE?", timeoutMs: 1200) ??
                          scpi.TryQuery(":TRIG:STATUS?", timeoutMs: 1200);

            if (!string.IsNullOrWhiteSpace(stat))
            {
                string s = stat.Trim().ToUpperInvariant();
                if (s.Contains("STOP") || s.Contains("HALT") || s.Contains("IDLE") || s.Contains("WAIT"))
                    runningGeneric = false;
                _logger.Debug("Trigger state: " + s);
            }
            else
            {
                _logger.Warn("No trigger state response; assuming running.");
            }

            if (runningGeneric)
            {
                if (!TryWrite(scpi, ":STOP")) TryWrite(scpi, ":ACQ:STATE 0");
                try { scpi.WaitOpc(1500); } catch { }
                System.Threading.Thread.Sleep(120);
                _logger.Debug("Acquisition stopped for screenshot.");
            }
            else
            {
                _logger.Debug("Acquisition already stopped.");
            }
            return runningGeneric;
        }

        private byte[] FetchScreenshot(ScpiClient scpi, ScopeVendor vendor)
        {
            if (vendor == ScopeVendor.Siglent)
            {
                var img = TryFetchSiglent(scpi);
                if (img != null) return img;
                _logger.Warn("Path failed (Siglent); falling back to generic list.");
            }
            return TryFetchRigolStyle(scpi);
        }

        private byte[] TryFetchRigolStyle(ScpiClient scpi)
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

            for (int i = 0; i < attempts.Length; i++)
            {
                string cmd = attempts[i];
                _logger.Debug("Trying screenshot command: " + cmd);

                int timeoutMs = (i == 0) ? 5000 : 8000;

                byte[] data = scpi.TryQueryBinaryBlock(cmd, 50 * 1024 * 1024, timeoutMs);
                if (data != null && data.Length > 64)
                {
                    _logger.Debug("Command " + cmd + " succeeded (block, bytes=" + data.Length + ", head=" + HexPreview(data, 16) + ")");
                    return data;
                }
                try { scpi.DrainInput(4096); } catch { }
            }
            return null;
        }

        private byte[] TryFetchSiglent(ScpiClient scpi)
        {
            string[] siglent = { "SCDP", "SCDP?", ":SCDP?" };
            foreach (var cmd in siglent)
            {
                _logger.Info("Trying screenshot command (Siglent): " + cmd);
                string mode;
                byte[] data = scpi.TryQuerySiglentImage(cmd, 50 * 1024 * 1024, 12000, out mode);
                if (data != null && data.Length > 64)
                {
                    _logger.Debug($"Command {cmd} succeeded (Siglent) (mode={mode}, bytes={data.Length}, head={HexPreview(data, 16)})");
                    return data;
                }
                _logger.Warn($"Command {cmd} produced no usable data (Siglent).");
            }
            return null;
        }

        private ImageKind DetectImageKind(byte[] raw)
        {
            if (raw.Length >= 8 && raw[0] == 0x89 && raw[1] == 0x50 && raw[2] == 0x4E && raw[3] == 0x47) return ImageKind.Png;
            if (raw.Length >= 2 && raw[0] == 0x42 && raw[1] == 0x4D) return ImageKind.Bmp;
            return ImageKind.Unknown;
        }

        private Image SaveAndReturnPreview(byte[] rawImage, string outputFileName)
        {
            var kind = DetectImageKind(rawImage);
            _logger.Debug($"Save: kind={kind}, len={rawImage?.Length}, head={HexPreview(rawImage, 16)}, path={outputFileName}");

            using (var ms = new MemoryStream(rawImage, false))
            {
                Image decoded = null;
                try
                {
                    decoded = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: true);
                }
                catch (Exception exDecode)
                {
                    _logger.Error("Decode failed before save: " + exDecode);
                    DumpRawOnFailure(rawImage, outputFileName, kind, "decode");
                    throw;
                }

                // Save as PNG
                using (decoded)
                {
                    using (var fs = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        decoded.Save(fs, ImageFormat.Png);
                        fs.Flush(true);
                    }

                    _logger.Highlight((File.Exists(outputFileName) ? "Saved " : "Created ") + Path.GetFullPath(outputFileName));
                    // Return a clone for UI
                    return (Image)decoded.Clone();
                }
            }
        }

        private void DumpRawOnFailure(byte[] raw, string targetPath, ImageKind kind, string stage)
        {
            try
            {
                string fallbackExt = (kind == ImageKind.Bmp) ? ".bmp" : (kind == ImageKind.Png) ? ".png" : ".bin";
                string dumpPath = Path.Combine(Path.GetDirectoryName(targetPath) ?? "", Path.GetFileNameWithoutExtension(targetPath) + $"_{stage}_raw" + fallbackExt);
                File.WriteAllBytes(dumpPath, raw);
                _logger.Warn("Fallback raw dump written: " + dumpPath);
            }
            catch (Exception ex)
            {
                _logger.Error("Fallback raw dump failed: " + ex);
            }
        }

        private void ForceAcquisition(ScpiClient scpi, bool forceResume = false)
        {
            try
            {
                if (DetectedVendor == ScopeVendor.Siglent)
                {
                    if (forceResume)
                    {
                        TryWrite(scpi, "RUN");
                        try { scpi.WaitOpc(1500); } catch { }
                        _logger.Info("Acquisition forced to RUN (Siglent).");
                        return;
                    }

                    if (!string.IsNullOrEmpty(_lastSiglentTrigMode))
                    {
                        if (_lastSiglentTrigMode == "AUTO" || _lastSiglentTrigMode == "NORM")
                        {
                            TryWrite(scpi, "TRIG_MODE " + _lastSiglentTrigMode);
                            TryWrite(scpi, "RUN");
                            scpi.WaitOpc(1500);
                            _logger.Info("Acquisition restored to " + _lastSiglentTrigMode + " (Siglent).");
                        }
                        else
                        {
                            _logger.Info("Previous mode " + _lastSiglentTrigMode + " (Siglent); leaving instrument stopped.");
                        }
                    }
                    else
                    {
                        TryWrite(scpi, "RUN");
                        _logger.Warn("Unknown previous mode (Siglent); issued RUN.");
                    }
                    return;
                }

                if (forceResume)
                {
                    if (!TryWrite(scpi, ":RUN")) TryWrite(scpi, ":ACQ:STATE 1");
                    try { scpi.WaitOpc(1500); } catch { }
                    _logger.Info("Acquisition forced to RUN.");
                    return;
                }

                if (!TryWrite(scpi, ":RUN")) TryWrite(scpi, ":ACQ:STATE 1");
                scpi.WaitOpc(1500);
                _logger.Notice("Acquisition resumed.");
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to resume acquisition: " + ex.Message);
            }
        }

        private void DrainInstrumentErrors(ScpiClient scpi)
        {
            for (int i = 0; i < 4; i++)
            {
                var line = scpi.TryQuery(":SYST:ERR?", timeoutMs: 800);
                if (string.IsNullOrWhiteSpace(line)) break;
                if (line.StartsWith("0,", StringComparison.Ordinal)) break;
                _logger.Warn("Instrument error: " + line.Trim());
            }
        }

        private bool ValidateBmp(byte[] data, out int width, out int height, out int bitsPerPixel, out string reason, bool patchHeader)
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
            uint compression = ReadUInt32LE(data, 30);

            if (planes != 1) { reason = "Planes != 1."; return false; }
            if (bitsPerPixel != 24 && bitsPerPixel != 16 && bitsPerPixel != 32) { reason = "Unsupported bpp " + bitsPerPixel; return false; }
            if (compression != 0 && !(compression == 3 && (bitsPerPixel == 16 || bitsPerPixel == 32))) { reason = "Unsupported compression mode " + compression; return false; }
            if (width <= 0 || Math.Abs(height) <= 0 || width > 10000 || Math.Abs(height) > 10000) { reason = "Unreasonable dimensions " + width + "x" + height; return false; }
            if (pixelDataOffset < 54 || pixelDataOffset > data.Length - 8) { reason = $"Suspicious pixel data offset {pixelDataOffset}"; return false; }

            int rowBytesRaw = (width * bitsPerPixel + 7) / 8;
            int stride = (rowBytesRaw + 3) & ~3;
            long expectedPixelsBytes = (long)stride * Math.Abs(height);
            long neededTotal = pixelDataOffset + expectedPixelsBytes;

            if (neededTotal > data.Length) { reason = $"Pixel data truncated. Need {neededTotal}, have {data.Length}"; return false; }

            // Accept and patch Rigol's "pixel-only" file size (FileSize + Offset == Actual)
            if (headerFileSize != data.Length)
            {
                long diff = (long)headerFileSize - data.Length;

                if (headerFileSize == 0 || headerFileSize + pixelDataOffset == data.Length)
                {
                    // Patch file size so GDI+ is happy
                    if (patchHeader)
                    {
                        _logger.Debug($"Non-standard BMP [FileSize] detected (header={headerFileSize}, actual={data.Length}, offset={pixelDataOffset}); patching header.");
                        PatchFileSize(data, (uint)data.Length);
                    }
                }
                else if (Math.Abs(diff) <= 32)
                {
                    _logger.Warn($"Minor [FileSize] mismatch tolerated. Header={headerFileSize}, Actual={data.Length}, Diff={diff}");
                }
                else
                {
                    reason = $"File size mismatch. Header={headerFileSize}, Actual={data.Length}, Diff={diff}";
                    return false;
                }
            }

            return true;
        }

        private void PatchFileSize(byte[] data, uint actual)
        {
            data[2] = (byte)(actual & 0xFF);
            data[3] = (byte)((actual >> 8) & 0xFF);
            data[4] = (byte)((actual >> 16) & 0xFF);
            data[5] = (byte)((actual >> 24) & 0xFF);
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

        private string EnsureDirectory(string finalPath)
        {
            var dir = Path.GetDirectoryName(finalPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return finalPath;
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

        // timebase helpers
        private static readonly double[] _timeDivSteps = new double[]
        {
            1e-9, 2e-9, 5e-9, 10e-9, 20e-9, 50e-9,
            100e-9, 200e-9, 500e-9, 1e-6, 2e-6, 5e-6,
            10e-6, 20e-6, 50e-6, 100e-6, 200e-6, 500e-6,
            1e-3, 2e-3, 5e-3, 10e-3, 20e-3, 50e-3,
            100e-3, 200e-3, 500e-3, 1, 2, 5, 10, 20, 50
        };

        private double QueryCurrentTimeDiv(ScpiClient scpi)
        {
            string raw = null;
            if (DetectedVendor == ScopeVendor.Siglent)
            {
                raw = scpi.TryQuery("TIME_DIV?", timeoutMs: 1200) ?? scpi.TryQuery("TDIV?", timeoutMs: 1200);
            }
            else
            {
                raw = scpi.TryQuery(":TIM:SCAL?", timeoutMs: 1200) ?? scpi.TryQuery(":TIMEBASE:SCAL?", timeoutMs: 1200);
            }
            if (string.IsNullOrWhiteSpace(raw)) return double.NaN;

            raw = raw.Trim().ToUpperInvariant();
            if (raw.EndsWith("S")) raw = raw.Substring(0, raw.Length - 1);

            double val;
            return double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val) ? val : double.NaN;
        }

        private bool SetTimeDiv(ScpiClient scpi, double seconds)
        {
            string val = seconds.ToString("0.###############E+0", System.Globalization.CultureInfo.InvariantCulture);
            bool ok;
            if (DetectedVendor == ScopeVendor.Siglent)
                ok = TryWrite(scpi, "TIME_DIV " + val) || TryWrite(scpi, "TDIV " + val);
            else
                ok = TryWrite(scpi, ":TIM:SCAL " + val) || TryWrite(scpi, ":TIMEBASE:SCAL " + val);

            if (ok) { try { scpi.WaitOpc(2000); } catch { } }
            return ok;
        }

        private int FindNearestTimeDivIndex(double current)
        {
            int best = -1; double bestDiff = double.MaxValue;
            for (int i = 0; i < _timeDivSteps.Length; i++)
            {
                double d = Math.Abs(_timeDivSteps[i] - current);
                if (d < bestDiff) { bestDiff = d; best = i; }
            }
            return best;
        }

        private string FormatSeconds(double seconds)
        {
            double abs = Math.Abs(seconds);
            string unit; double value;
            if (abs < 1e-9) { value = seconds * 1e12; unit = "ps"; }
            else if (abs < 1e-6) { value = seconds * 1e9; unit = "ns"; }
            else if (abs < 1e-3) { value = seconds * 1e6; unit = "µs"; }
            else if (abs < 1) { value = seconds * 1e3; unit = "ms"; }
            else { value = seconds; unit = "s"; }
            return value.ToString(value < 10 ? "0.###" : "0.#", System.Globalization.CultureInfo.InvariantCulture) + unit;
        }

        // snapshot/resume helpers
        private bool QueryIsRunning(ScpiClient scpi, int statusTimeoutMs = 800)
        {
            try
            {
                if (DetectedVendor == ScopeVendor.Siglent)
                {
                    string mode = scpi.TryQuery("TRIG_MODE?", timeoutMs: statusTimeoutMs);
                    if (string.IsNullOrWhiteSpace(mode)) return true;
                    mode = mode.Trim().ToUpperInvariant();
                    return mode == "AUTO" || mode == "NORM";
                }
                else
                {
                    string stat = scpi.TryQuery(":TRIG:STAT?", timeoutMs: statusTimeoutMs) ??
                                  scpi.TryQuery(":TRIG:STATE?", timeoutMs: statusTimeoutMs) ??
                                  scpi.TryQuery(":TRIG:STATUS?", timeoutMs: statusTimeoutMs);
                    if (string.IsNullOrWhiteSpace(stat)) return true;
                    stat = stat.Trim().ToUpperInvariant();
                    if (stat.Contains("STOP") || stat.Contains("HALT") || stat.Contains("IDLE") || stat.Contains("WAIT"))
                        return false;
                    return true;
                }
            }
            catch { return true; }
        }

        private bool StopAcq(ScpiClient scpi)
        {
            if (DetectedVendor == ScopeVendor.Siglent) return TryWrite(scpi, "STOP");
            return TryWrite(scpi, ":STOP") || TryWrite(scpi, ":ACQ:STATE 0");
        }

        private bool RunAcq(ScpiClient scpi)
        {
            if (DetectedVendor == ScopeVendor.Siglent) return TryWrite(scpi, "RUN");
            return TryWrite(scpi, ":RUN") || TryWrite(scpi, ":ACQ:STATE 1");
        }

        private bool ForceRun(ScpiClient scpi)
        {
            bool ok;
            if (DetectedVendor == ScopeVendor.Siglent)
            {
                ok = TryWrite(scpi, "RUN");
                if (!QueryIsRunning(scpi))
                {
                    TryWrite(scpi, "TRIG_MODE AUTO");
                    System.Threading.Thread.Sleep(50);
                    ok = TryWrite(scpi, "RUN") || ok;
                }
                return ok;
            }
            ok = TryWrite(scpi, ":RUN");
            if (!ok) ok = TryWrite(scpi, "RUN");
            if (!ok) ok = TryWrite(scpi, ":ACQ:STATE 1");
            return ok;
        }

        private bool WaitUntilRunning(ScpiClient scpi, int timeoutMs, int statusTimeoutMs = 200, int pollDelayMs = 50)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (QueryIsRunning(scpi, statusTimeoutMs)) return true;
                System.Threading.Thread.Sleep(pollDelayMs);
            }
            return QueryIsRunning(scpi, statusTimeoutMs);
        }

        private bool TryWrite(ScpiClient scpi, string cmd)
        {
            try { scpi.WriteLine(cmd); return true; } catch { return false; }
        }

        // --------------- minimal SCPI client -----------------

        private sealed class ScpiClient : IDisposable
        {
            private readonly TcpClient _client;
            private readonly NetworkStream _stream;
            private readonly byte[] _one = new byte[1];

            private ScpiClient(TcpClient client)
            {
                _client = client;
                _stream = client.GetStream();
            }

            public static ScpiClient Connect(string host, int port, int connectTimeoutMs, int ioTimeoutMs)
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
                return new ScpiClient(client);
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

                    if (header.Count >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
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

            public void Dispose()
            {
                try { _stream.Dispose(); } catch { }
                try { _client.Close(); } catch { }
            }
        }

        // little-endian helpers
        private static uint ReadUInt32LE(byte[] d, int o) => (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));
        private static int ReadInt32LE(byte[] d, int o) => d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24);
        private static ushort ReadUInt16LE(byte[] d, int o) => (ushort)(d[o] | (d[o + 1] << 8));
    }
}