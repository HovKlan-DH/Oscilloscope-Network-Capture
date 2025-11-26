using System;
using System.IO;
using System.Xml.Serialization;

namespace Oscilloscope_Network_Capture.Core.Configuration
{
    public static class ConfigurationService
    {
        private static string ConfigDirectory;
        private static string ConfigPath;

        public static bool Exists => File.Exists(ConfigPath);

        // Simple dirtiness flag for optional prompts
        public static bool Dirty { get; private set; }

        static ConfigurationService()
        {
            // Place configuration alongside the executable
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            ConfigDirectory = baseDir;
            Directory.CreateDirectory(ConfigDirectory);
            ConfigPath = Path.Combine(ConfigDirectory, "Oscilloscope-Network-Capture.cfg");
            Dirty = false;
        }

        // Allow overriding the configuration file path at startup
        public static void SetConfigFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            string resolved = path;
            if (!Path.IsPathRooted(resolved))
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                resolved = Path.Combine(baseDir, resolved);
            }

            try
            {
                var dir = Path.GetDirectoryName(resolved);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch
            {
                // Ignore directory creation errors; Save will try again
            }

            ConfigDirectory = Path.GetDirectoryName(resolved) ?? AppDomain.CurrentDomain.BaseDirectory;
            ConfigPath = resolved;
        }

        public static AppConfiguration Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    using (var fs = File.OpenRead(ConfigPath))
                    {
                        var xs = new XmlSerializer(typeof(AppConfiguration));
                        return (AppConfiguration)xs.Deserialize(fs);
                    }
                }
            }
            catch
            {
                // Ignore and return defaults
            }

            return new AppConfiguration();
        }

        public static void Save(AppConfiguration config)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var fs = File.Create(ConfigPath))
                {
                    var xs = new XmlSerializer(typeof(AppConfiguration));
                    xs.Serialize(fs, config);
                }
                Dirty = false;
            }
            catch
            {
                // Ignore persist errors
            }
        }

        public static string GetConfigFilePath() => ConfigPath;
    }
}