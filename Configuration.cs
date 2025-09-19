using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;

namespace Oscilloscope_Network_Capture
{
    public sealed class Configuration
    {
        private readonly string _configPath;
        private readonly Logger _logger;

        public Configuration(string configPath, Logger logger)
        {
            _configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
            _logger = logger ?? new Logger();
        }

        // Persisted values
        public string ScopeIp { get; set; } = "192.168.0.100";
        public int ScopePort { get; set; } = 5555;
        public string Region { get; set; } = "";
        public string Component { get; set; } = "U1";
        public string FilenameFormat { get; set; } = "{Component}_{Number}_{Region}_{Date}_{Time}";
        public string OutputFolder { get; set; } = "output";
        public bool BeepEnabled { get; set; } = true;
        public bool ForceAcquisition { get; set; } = false;
        public int? SplitterDistance { get; set; }
        public bool WindowMaximized { get; set; } = false;

        public void Load()
        {
            try
            {
                if (!File.Exists(_configPath)) return;

                var lines = File.ReadAllLines(_configPath);
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool kv = false;

                foreach (var raw in lines)
                {
                    var line = (raw ?? "").Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        kv = true;
                        map[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                    }
                }

                if (!kv)
                {
                    // Legacy: content only IP
                    var txt = string.Join("", lines).Trim();
                    if (!string.IsNullOrWhiteSpace(txt) && IPAddress.TryParse(txt, out _))
                    {
                        ScopeIp = txt;
                        _logger.Warn("Loaded legacy IP [" + ScopeIp + "] from configuration file.");
                    }
                    return;
                }

                if (map.TryGetValue("IP", out var ip) && IPAddress.TryParse(ip, out _)) ScopeIp = ip;
                if (map.TryGetValue("Port", out var portStr) && int.TryParse(portStr, out var p) && p > 0 && p <= 65535) ScopePort = p;
                if (map.TryGetValue("Region", out var region)) Region = region ?? "";
                if (map.TryGetValue("Component", out var comp) && !string.IsNullOrWhiteSpace(comp)) Component = comp.Trim();
                if (map.TryGetValue("FilenameFormat", out var fmt) && !string.IsNullOrWhiteSpace(fmt)) FilenameFormat = fmt;
                if (map.TryGetValue("OutputFolder", out var outFolder) && !string.IsNullOrWhiteSpace(outFolder)) OutputFolder = NormalizeOutputFolder(outFolder);

                if (map.TryGetValue("Beep", out var beepStr))
                    BeepEnabled = beepStr == "1" || beepStr.Equals("true", StringComparison.OrdinalIgnoreCase) || beepStr.Equals("yes", StringComparison.OrdinalIgnoreCase);

                if (map.TryGetValue("ForceAcquisition", out var resumeStr))
                    ForceAcquisition = resumeStr == "1" || resumeStr.Equals("true", StringComparison.OrdinalIgnoreCase) || resumeStr.Equals("yes", StringComparison.OrdinalIgnoreCase);

                if (map.TryGetValue("SplitterDistance", out var splitStr) && int.TryParse(splitStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sd))
                    SplitterDistance = sd;

                if (map.TryGetValue("WindowMaximized", out var winMax))
                    WindowMaximized = winMax == "1" || winMax.Equals("true", StringComparison.OrdinalIgnoreCase) || winMax.Equals("yes", StringComparison.OrdinalIgnoreCase);

                _logger.Debug($"Loaded configuration:");
                _logger.Debug($"    IP: {ScopeIp}:{ScopePort}");
                _logger.Debug($"    Region: {Region}");
                _logger.Debug($"    Component: {Component}");
                _logger.Debug($"    FilenameFormat: {FilenameFormat}");
                _logger.Debug($"    OutputFolder: {OutputFolder}");
                _logger.Debug($"    Beep: {(BeepEnabled ? 1 : 0)}");
                _logger.Debug($"    ForceAcquisition: {(ForceAcquisition ? 1 : 0)}");
                _logger.Debug($"    SplitterDistance: {(SplitterDistance ?? 0)}");
                _logger.Debug($"    WindowMaximized: {(WindowMaximized ? 1 : 0)}");
            }
            catch (Exception ex)
            {
                _logger.Error("Warning: Could not load configuration file: " + ex.Message);
            }
        }

        public void Save()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("IP=" + ScopeIp);
                sb.AppendLine("Port=" + ScopePort);
                sb.AppendLine("Region=" + (Region ?? ""));
                sb.AppendLine("Component=" + Component);
                sb.AppendLine("FilenameFormat=" + FilenameFormat);
                sb.AppendLine("OutputFolder=" + OutputFolder);
                sb.AppendLine("Beep=" + (BeepEnabled ? "1" : "0"));
                sb.AppendLine("ForceAcquisition=" + (ForceAcquisition ? "1" : "0"));
                sb.AppendLine("SplitterDistance=" + (SplitterDistance?.ToString() ?? "0"));
                sb.AppendLine("WindowMaximized=" + (WindowMaximized ? "1" : "0"));

                File.WriteAllText(_configPath, sb.ToString());

                _logger.Debug($"Saved configuration:");
                _logger.Debug($"    IP: {ScopeIp}:{ScopePort}");
                _logger.Debug($"    Region: {Region}");
                _logger.Debug($"    Component: {Component}");
                _logger.Debug($"    FilenameFormat: {FilenameFormat}");
                _logger.Debug($"    OutputFolder: {OutputFolder}");
                _logger.Debug($"    Beep: {(BeepEnabled ? 1 : 0)}");
                _logger.Debug($"    ForceAcquisition: {(ForceAcquisition ? 1 : 0)}");
                _logger.Debug($"    SplitterDistance: {(SplitterDistance ?? 0)}");
                _logger.Debug($"    WindowMaximized: {(WindowMaximized ? 1 : 0)}");
            }
            catch (Exception ex)
            {
                _logger.Error("Warning: Could not save configuration file: " + ex.Message);
            }
        }

        public void EnsureDefaultOutputFolderResolved()
        {
            var before = OutputFolder;

            if (string.IsNullOrWhiteSpace(OutputFolder) ||
                string.Equals(OutputFolder, "output", StringComparison.OrdinalIgnoreCase))
            {
                var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(exeDir))
                    exeDir = AppDomain.CurrentDomain.BaseDirectory;

                OutputFolder = Path.Combine(exeDir, "output");
            }

            OutputFolder = NormalizeOutputFolder(OutputFolder);

            try
            {
                if (!Directory.Exists(OutputFolder))
                    Directory.CreateDirectory(OutputFolder);
            }
            catch (Exception ex)
            {
                _logger.Error("Could not create output folder: " + ex.Message);
            }

            if (!string.Equals(before, OutputFolder, StringComparison.OrdinalIgnoreCase))
                Save();
        }

        public string NormalizeOutputFolder(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                raw = "output";

            raw = raw.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            try
            {
                if (Path.IsPathRooted(raw))
                    return Path.GetFullPath(raw);
            }
            catch { }

            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(baseDir))
                baseDir = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                return Path.GetFullPath(Path.Combine(baseDir, raw));
            }
            catch
            {
                return Path.Combine(baseDir, "output");
            }
        }
    }
}