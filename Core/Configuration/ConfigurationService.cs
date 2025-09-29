using System;
using System.IO;
using System.Xml.Serialization;

namespace Oscilloscope_Network_Capture.Core.Configuration
{
    public static class ConfigurationService
    {
        private static readonly string ConfigDirectory;
        private static readonly string ConfigPath;

        public static bool Exists => File.Exists(ConfigPath);

        // Simple dirtiness flag for optional prompts
        public static bool Dirty { get; private set; }

        static ConfigurationService()
        {
            // Place configuration alongside the executable
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            ConfigDirectory = baseDir;
            Directory.CreateDirectory(ConfigDirectory);
            ConfigPath = Path.Combine(ConfigDirectory, "config.xml");
            Dirty = false;
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

        public static void ResetToDefaults()
        {
            var fresh = new AppConfiguration();
            Save(fresh);
        }

        public static string GetConfigFilePath() => ConfigPath;
    }
}
