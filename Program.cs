using System;
using System.IO;
using System.Windows.Forms;
using Oscilloscope_Network_Capture.Core.Configuration;

namespace Oscilloscope_Network_Capture
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Optional single argument: alternative configuration file path/name
            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                var candidate = args[0];
                ConfigurationService.SetConfigFilePath(candidate);

                // If the specified config file is missing, inform the user and continue with defaults
                var resolved = ConfigurationService.GetConfigFilePath();
                if (!File.Exists(resolved))
                {
                    try
                    {
                        MessageBox.Show(
                            "The specified configuration file was not found:\r\n\r\n[" + resolved +
                            "]\r\n\r\nPlease remove the configuration file parameter and try again.",
                            "Configuration",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    catch
                    {
                        // best-effort
                    }
                } else
                {
                    Application.Run(new Main());
                }
            } else
            {
                Application.Run(new Main());
            }
        }
    }
}