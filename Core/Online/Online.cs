using Oscilloscope_Network_Capture.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Net;
using System.Linq;

namespace Oscilloscope_Network_Capture.Core.Online
{
    public static class Online
    {
        private const string FeedbackUrl = "https://commodore-repair-toolbox.dk/feedback-app/";
        private const string UpdateUrl = "https://commodore-repair-toolbox.dk/auto-update/";
        private const string ShareUploadUrl = "https://commodore-repair-toolbox.dk/onc/upload/";
        public const string ShareGalleryUrl = "https://commodore-repair-toolbox.dk/onc/gallery/";

        // Current running app version string
        public static string CurrentVersion = "";
        private static string UserAgent = "";

        public static async Task<string> SendShareAsync(
    string vendor,
    string model,
    IEnumerable<string> filePaths,
    IProgress<(long Sent, long Total)> progress,
    CancellationToken token)
        {
            if (filePaths == null) throw new ArgumentNullException(nameof(filePaths));

            var paths = filePaths
                .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                .ToList();

            if (paths.Count == 0) return string.Empty;

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };

            using (var client = new HttpClient(handler, disposeHandler: true))
            {
                UserAgent = "ONC " + Main.versionThis;

                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                long totalFileBytes = 0;
                foreach (var p in paths)
                {
                    try { totalFileBytes += Math.Max(0, new FileInfo(p).Length); } catch { }
                }

                using (var multipart = new MultipartFormDataContent())
                {
                    multipart.Add(new StringContent(vendor ?? string.Empty, Encoding.UTF8), "vendor");
                    multipart.Add(new StringContent(model ?? string.Empty, Encoding.UTF8), "model");

                    foreach (var path in paths)
                    {
                        FileInfo fi;
                        try { fi = new FileInfo(path); }
                        catch { continue; }

                        var stream = File.OpenRead(fi.FullName);
                        var fileContent = new StreamContent(stream);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GuessMimeTypeFromFileName(fi.Name));

                        // IMPORTANT: this is what makes PHP build $_FILES['files'][name][0..n]
                        multipart.Add(fileContent, "files[]", fi.Name);
                    }

                    using (var content = new ProgressableStreamContent(multipart, progress, totalFileBytes))
                    using (var resp = await client.PostAsync(ShareUploadUrl, content, token).ConfigureAwait(false))
                    {
                        string body = string.Empty;
                        try { body = (await resp.Content.ReadAsStringAsync().ConfigureAwait(false))?.Trim() ?? string.Empty; } catch { }
                        return body;
                    }
                }
            }
        }

        private sealed class ProgressableStreamContent : HttpContent
        {
            private const int DefaultBufferSize = 81920;

            private readonly HttpContent _inner;
            private readonly IProgress<(long Sent, long Total)> _progress;
            private readonly long _totalBytes;

            public ProgressableStreamContent(HttpContent inner, IProgress<(long Sent, long Total)> progress, long totalBytes)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _progress = progress;
                _totalBytes = totalBytes;

                foreach (var h in _inner.Headers)
                    Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                long uploaded = 0;

                using (var input = await _inner.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var buffer = new byte[DefaultBufferSize];
                    int read;

                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                        uploaded += read;

                        _progress?.Report((uploaded, _totalBytes));
                    }
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                // IMPORTANT:
                // We cannot reliably compute the actual multipart Content-Length here.
                // Returning an incorrect length causes:
                // "Bytes to be written to the stream exceed the Content-Length bytes size specified."
                length = 0;
                return false;
            }
        }

        private static string GuessMimeTypeFromFileName(string fileName)
        {
            var ext = Path.GetExtension(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(ext)) return "application/octet-stream";

            switch (ext.ToLowerInvariant())
            {
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".bmp": return "image/bmp";
                case ".gif": return "image/gif";
                case ".webp": return "image/webp";
                default: return "application/octet-stream";
            }
        }

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
