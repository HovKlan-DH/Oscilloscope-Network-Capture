using Oscilloscope_Network_Capture.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Oscilloscope_Network_Capture.Core.Online
{
    public static class Online
    {
        private const string FeedbackUrl = "https://commodore-repair-toolbox.dk/feedback-app/";
        private const string UpdateUrl = "https://commodore-repair-toolbox.dk/auto-update/";

        // Current running app version string
        public static string CurrentVersion = "";
        private static string UserAgent = "";

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true; // optional
            try
            {
                var rx = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
                return rx.IsMatch(email.Trim());
            }
            catch
            {
                return false;
            }
        }

        public static string SerializeConfig(AppConfiguration cfg)
        {
            if (cfg == null) return string.Empty;
            try
            {
                var xs = new XmlSerializer(typeof(AppConfiguration));
                using (var ms = new MemoryStream())
                using (var writer = new StreamWriter(ms, new UTF8Encoding(false)))
                {
                    xs.Serialize(writer, cfg);
                    writer.Flush();
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                return "<serialize-error>" + ex.Message + "</serialize-error>";
            }
        }

        public static string ReadDebugLog()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Oscilloscope-Network-Capture.log");
                if (!File.Exists(path)) return string.Empty;
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return "<read-log-error>" + ex.Message + "</read-log-error>";
            }
        }

        public static async Task<string> SendFeedbackAsync(string configXml, string debugText, string email, string version, string feedback, CancellationToken token)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false // do not follow redirects that could turn POST into GET
            };

            using (var client = new HttpClient(handler, disposeHandler: true))
            {
                UserAgent = "ONC " + Main.versionThis;

                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                var pairs = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("config", configXml ?? string.Empty),
            new KeyValuePair<string, string>("debug", debugText ?? string.Empty),
            new KeyValuePair<string, string>("version", version ?? string.Empty),
            new KeyValuePair<string, string>("email", email ?? string.Empty),
            new KeyValuePair<string, string>("feedback", feedback ?? string.Empty),
        };
                var content = new FormUrlEncodedContent(pairs);

                using (var resp = await client.PostAsync(FeedbackUrl, content, token).ConfigureAwait(false))
                {
                    string body = string.Empty;
                    try { body = (await resp.Content.ReadAsStringAsync().ConfigureAwait(false))?.Trim() ?? string.Empty; } catch { }
                    return body; // caller decides if not equal to "Success"
                }
            }
        }

        public static async Task<string> GetNewestVersionAsync(CancellationToken token)
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using (var client = new HttpClient(handler, disposeHandler: true))
            {
                UserAgent = "ONC "+ Main.versionThis;

                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("control","ONC")
                });

                var resp = await client.PostAsync(UpdateUrl, content, token).ConfigureAwait(false);
                string body = string.Empty;
                try { body = (await resp.Content.ReadAsStringAsync().ConfigureAwait(false))?.Trim() ?? string.Empty; } catch { }
                if (string.IsNullOrEmpty(body)) return string.Empty;

                var m = Regex.Match(body, @"Version\s*:\s*(.+)", RegexOptions.IgnoreCase);
                return m.Success ? m.Groups[1].Value.Trim() : body;
            }
        }
    }
}
