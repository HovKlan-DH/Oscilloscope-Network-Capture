using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    // Enum of known built-in test suites that wire up existing test buttons
    public enum ScopeTestSuite
    {
        QueryIdentify,
        PopLastSystemError,
        OperationComplete,
        ClearStatistics,
        QueryActiveTrigger,
        Stop,
        Single,
        Run,
        QueryTriggerMode,
        QueryTriggerLevel,
        SetTriggerLevel,
        QueryTimeDiv,
        SetTimeDiv,
        DumpImage
    }

    public static class ScopeTestSuiteRegistry
    {
        // Human-friendly names for logging
        private static readonly Dictionary<ScopeTestSuite, string> Names = new Dictionary<ScopeTestSuite, string>
        {
            { ScopeTestSuite.QueryIdentify, "Query identify instrument" },
            { ScopeTestSuite.ClearStatistics, "Set \"Clear Statistics\"" },
            { ScopeTestSuite.QueryActiveTrigger, "Query active trigger" },
            { ScopeTestSuite.Stop, "Set \"Stop\" mode" },
            { ScopeTestSuite.Run, "Set \"Run\" mode" },
            { ScopeTestSuite.Single, "Set \"Single\" mode" },
            { ScopeTestSuite.QueryTriggerMode, "Query trigger mode" },
            { ScopeTestSuite.QueryTriggerLevel, "Query trigger level" },
            { ScopeTestSuite.SetTriggerLevel, "Set trigger level" },
            { ScopeTestSuite.QueryTimeDiv, "Query TIME/DIV" },
            { ScopeTestSuite.SetTimeDiv, "Set TIME/DIV" },
            { ScopeTestSuite.DumpImage, "Query dump image" },
            { ScopeTestSuite.PopLastSystemError, "Query last system error" },
            { ScopeTestSuite.OperationComplete, "Query \"Operation Complete\"" },
        };

        // Built-in defaults mapping suite id -> ordered ScopeCommand steps
        private static readonly Dictionary<ScopeTestSuite, ScopeCommand[]> Defaults = new Dictionary<ScopeTestSuite, ScopeCommand[]>
        {
            { ScopeTestSuite.QueryIdentify, new[]{
                ScopeCommand.Identify
            }},
            { ScopeTestSuite.PopLastSystemError, new[]{
                ScopeCommand.PopLastSystemError
            }},
            { ScopeTestSuite.OperationComplete, new[]{
                ScopeCommand.OperationComplete
            }},
            { ScopeTestSuite.ClearStatistics, new[]{
                ScopeCommand.ClearStatistics,
                ScopeCommand.OperationComplete,
                ScopeCommand.PopLastSystemError
            }},
            { ScopeTestSuite.QueryActiveTrigger, new[]{
                ScopeCommand.QueryActiveTrigger
            }},
            { ScopeTestSuite.Stop, new[]{
                ScopeCommand.Stop,
                ScopeCommand.OperationComplete,
                ScopeCommand.PopLastSystemError
            }},
            { ScopeTestSuite.Single, new[]{
                ScopeCommand.Single,
                ScopeCommand.OperationComplete,
                ScopeCommand.PopLastSystemError
            }},
            { ScopeTestSuite.Run, new[]{
                ScopeCommand.Run,
                ScopeCommand.OperationComplete,
                ScopeCommand.PopLastSystemError
            }},
            { ScopeTestSuite.QueryTriggerMode, new[]{
                ScopeCommand.QueryTriggerMode
            }},
            { ScopeTestSuite.QueryTriggerLevel, new[]{
                ScopeCommand.QueryTriggerLevel
            }},
            { ScopeTestSuite.SetTriggerLevel, new[]{
                ScopeCommand.SetTriggerLevel,
                ScopeCommand.OperationComplete,
                ScopeCommand.PopLastSystemError
            }},
            { ScopeTestSuite.QueryTimeDiv, new[]{
                ScopeCommand.QueryTimeDiv
            }},
            { ScopeTestSuite.SetTimeDiv, new[]{
                ScopeCommand.SetTimeDiv,
                ScopeCommand.OperationComplete,
                ScopeCommand.PopLastSystemError
            }},
            { ScopeTestSuite.DumpImage, new[]{
                ScopeCommand.DumpImage
            }},
        };

        public static string GetDisplayName(ScopeTestSuite suite)
            => Names.TryGetValue(suite, out var n) ? n : suite.ToString();

        // Resolve a suite to an ordered command list, allowing overrides from AppConfiguration
        public static IReadOnlyList<ScopeCommand> Resolve(Oscilloscope_Network_Capture.Core.Configuration.AppConfiguration config, ScopeTestSuite suite)
        {
            if (config != null && config.ScpiTestSuites != null)
            {
                var ov = config.ScpiTestSuites.FirstOrDefault(s => string.Equals(s.Id, suite.ToString(), StringComparison.OrdinalIgnoreCase));
                if (ov != null && ov.Steps != null && ov.Steps.Count > 0)
                {
                    var parsed = new List<ScopeCommand>();
                    foreach (var name in ov.Steps)
                    {
                        if (Enum.TryParse<ScopeCommand>(name, ignoreCase: true, out var cmd)) parsed.Add(cmd);
                    }
                    if (parsed.Count > 0) return parsed;
                }
            }
            return Defaults[suite];
        }

        // ---------------- Concurrency helpers: run one suite at a time ----------------

        // ---------------- Image helpers for DumpImage ----------------

        public static bool TryGetImageInfoFromScpiBlock(byte[] scpiBlock, out int width, out int height, out int bitsPerPixel, out int fileSizeBytes)
        {
            width = 0;
            height = 0;
            bitsPerPixel = 0;
            fileSizeBytes = 0;

            if (scpiBlock == null || scpiBlock.Length == 0)
                return false;

            int payloadStart;
            int payloadLen;
            if (!TrySliceScpiPayload(scpiBlock, out payloadStart, out payloadLen))
                return false;

            fileSizeBytes = payloadLen;

            // Basic BMP validation
            if (payloadLen < 54) return false;
            if (scpiBlock[payloadStart + 0] != (byte)'B' || scpiBlock[payloadStart + 1] != (byte)'M')
                return false;

            int dibSize = BitConverter.ToInt32(scpiBlock, payloadStart + 14);
            if (dibSize < 40) return false; // Expect BITMAPINFOHEADER or larger

            short planes = BitConverter.ToInt16(scpiBlock, payloadStart + 26);
            if (planes != 1) return false;

            short bpp = BitConverter.ToInt16(scpiBlock, payloadStart + 28);
            bitsPerPixel = bpp;

            int w = BitConverter.ToInt32(scpiBlock, payloadStart + 18);
            int h = BitConverter.ToInt32(scpiBlock, payloadStart + 22);
            // Height can be negative (top-down). Report absolute value.
            width = w < 0 ? -w : w;
            height = h < 0 ? -h : h;

            return width > 0 && height > 0 && bitsPerPixel > 0;
        }

        // Returns: "Dumped image (800x480px, 24bpp BMP, 1125.1 KB)" or a size-only fallback.
        public static string FormatDumpImageInfo(byte[] scpiBlock)
        {
            int payloadStart, payloadLen;
            if (!TrySliceScpiPayload(scpiBlock, out payloadStart, out payloadLen))
                return "Dumped image.";

            // Detect container by signature
            string container = "binary";
            if (payloadLen >= 2 && scpiBlock[payloadStart + 0] == (byte)'B' && scpiBlock[payloadStart + 1] == (byte)'M') container = "BMP";
            else if (payloadLen >= 8 && scpiBlock[payloadStart + 0] == 0x89 && scpiBlock[payloadStart + 1] == 0x50 && scpiBlock[payloadStart + 2] == 0x4E && scpiBlock[payloadStart + 3] == 0x47) container = "PNG";
            else if (payloadLen >= 2 && scpiBlock[payloadStart + 0] == 0xFF && scpiBlock[payloadStart + 1] == 0xD8) container = "JPEG";
            else if (payloadLen >= 4 && scpiBlock[payloadStart + 0] == (byte)'G' && scpiBlock[payloadStart + 1] == (byte)'I' && scpiBlock[payloadStart + 2] == (byte)'F' && scpiBlock[payloadStart + 3] == (byte)'8') container = "GIF";
            else if (payloadLen >= 12 &&
                     scpiBlock[payloadStart + 0] == (byte)'R' && scpiBlock[payloadStart + 1] == (byte)'I' &&
                     scpiBlock[payloadStart + 2] == (byte)'F' && scpiBlock[payloadStart + 3] == (byte)'F' &&
                     scpiBlock[payloadStart + 8] == (byte)'W' && scpiBlock[payloadStart + 9] == (byte)'E' &&
                     scpiBlock[payloadStart + 10] == (byte)'B' && scpiBlock[payloadStart + 11] == (byte)'P')
                container = "WEBP";

            // Try BMP header to extract w/h/bpp (payload size is the bytes saved)
            int w, h, bpp, fileSizeBytes;
            if (TryGetImageInfoFromScpiBlock(scpiBlock, out w, out h, out bpp, out fileSizeBytes))
            {
                double kib = fileSizeBytes / 1024.0;
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Dumped image ({0}x{1}px, {2}bpp {3}, {4:F1} KB)", w, h, bpp, container, kib);
            }

            // Fallback: report size only
            double sizeOnlyKiB = payloadLen / 1024.0;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Dumped image ({0:F1} KB)", sizeOnlyKiB);
        }

        // Parses SCPI definite-length binary block framing. If no SCPI header, returns the whole buffer.
        private static bool TrySliceScpiPayload(byte[] buffer, out int payloadStart, out int payloadLen)
        {
            payloadStart = 0;
            payloadLen = 0;

            if (buffer == null) return false;
            if (buffer.Length >= 3 && buffer[0] == (byte)'#')
            {
                int ndigits = buffer[1] - (int)'0';
                if (ndigits < 1 || ndigits > 9) return false;
                if (buffer.Length < 2 + ndigits) return false;

                int len = 0;
                for (int i = 0; i < ndigits; i++)
                {
                    int d = buffer[2 + i] - (int)'0';
                    if (d < 0 || d > 9) return false;
                    len = len * 10 + d;
                }

                int start = 2 + ndigits;
                if (start + len > buffer.Length) return false;

                payloadStart = start;
                payloadLen = len;
                return true;
            }

            // No SCPI framing; treat entire buffer as payload
            payloadStart = 0;
            payloadLen = buffer.Length;
            return true;
        }

        // Global gate to serialize suite execution
        private static readonly SemaphoreSlim SuiteGate = new SemaphoreSlim(1, 1);

        // Query-only flag (non-authoritative, but useful for UI state)
        public static bool IsSuiteRunning => SuiteGate.CurrentCount == 0;

        // Queueing behavior: callers await here and will run exclusively when the gate is free.
        public static async Task RunExclusiveAsync(Func<Task> action, CancellationToken cancellationToken = default(CancellationToken))
        {
            await SuiteGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                SuiteGate.Release();
            }
        }

        // Queueing behavior with a result
        public static async Task<T> RunExclusiveAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default(CancellationToken))
        {
            await SuiteGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                SuiteGate.Release();
            }
        }

        // Reject-if-busy behavior: returns false if another suite is running.
        public static bool TryBeginSuite(out IDisposable lease)
        {
            if (SuiteGate.Wait(0))
            {
                lease = new Releaser(SuiteGate);
                return true;
            }
            lease = null;
            return false;
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _sem;
            private int _disposed;
            public Releaser(SemaphoreSlim sem) { _sem = sem; }
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    _sem.Release();
            }
        }

        // ---------------- Single entry point to run suites exclusively ----------------

        // Embed all suite calls here: resolve and execute steps behind the global gate.
        // 'runner' is your delegate that actually performs a ScopeCommand against the scope.
        public static async Task RunSuiteExclusiveAsync(
            Oscilloscope_Network_Capture.Core.Configuration.AppConfiguration config,
            ScopeTestSuite suite,
            Func<ScopeCommand, CancellationToken, Task> runner,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            await RunExclusiveAsync(async () =>
            {
                var steps = Resolve(config, suite);
                for (int i = 0; i < steps.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await runner(steps[i], cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        // Variant that does not queue; returns false if a suite is already running.
        public static async Task<bool> TryRunSuiteExclusiveAsync(
            Oscilloscope_Network_Capture.Core.Configuration.AppConfiguration config,
            ScopeTestSuite suite,
            Func<ScopeCommand, CancellationToken, Task> runner,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            IDisposable lease;
            if (!TryBeginSuite(out lease))
                return false;

            using (lease)
            {
                var steps = Resolve(config, suite);
                for (int i = 0; i < steps.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await runner(steps[i], cancellationToken).ConfigureAwait(false);
                }
                return true;
            }
        }
    }
}
