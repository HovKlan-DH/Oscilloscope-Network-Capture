using Oscilloscope_Network_Capture.Core.Logging;
using Oscilloscope_Network_Capture.Core.Transport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public abstract class ScpiScopeBase : IScope
    {
        public string Vendor { get; protected set; }
        public string Model { get; protected set; }
        public string Resource { get; set; }

        protected readonly IInstrumentTransport Transport;

        protected ScpiScopeBase(IInstrumentTransport transport)
        {
            Transport = transport;
        }

        public bool IsConnected => Transport?.IsConnected ?? false;

        public virtual async Task ConnectAsync(CancellationToken ct = default(CancellationToken))
        {
            if (Transport == null) throw new InvalidOperationException("Transport not configured.");
            await Transport.ConnectAsync(Resource, 5000, ct).ConfigureAwait(false);
        }

        public virtual Task DisconnectAsync() => Transport.DisconnectAsync();

        protected string Cmd(ScopeCommand cmd) => FormatCommand(cmd);
        protected string Cmd(ScopeCommand cmd, object arg) => FormatCommand(cmd, arg);

        private string FormatCommand(ScopeCommand cmd, params object[] args)
        {
            var profileSpecific = ScpiProfileRegistry.Find(Vendor, Model);
            if (profileSpecific != null && profileSpecific.TryGet(cmd, out var scpi))
            {
                return args == null || args.Length == 0 ? scpi : string.Format(CultureInfo.InvariantCulture, scpi, args);
            }
            var profileDefault = ScpiProfileRegistry.Find(Vendor, "*");
            if (profileDefault != null && profileDefault.TryGet(cmd, out scpi))
            {
                return args == null || args.Length == 0 ? scpi : string.Format(CultureInfo.InvariantCulture, scpi, args);
            }
            throw new NotSupportedException($"SCPI command '{cmd}' not defined for {Vendor} {Model}.");
        }

        private static string Trunc(string s, int max = 200)
        {
            if (s == null) return string.Empty;
            s = s.Replace("\r", "\\r").Replace("\n", "\\n");
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        public virtual async Task<string> IdentifyAsync(CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.Identify);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            var resp = await Transport.QueryAsync(scpi, 5000, ct).ConfigureAwait(false);
            Logger.Instance.Debug($"SCPI << {Trunc(resp)}");
            return resp;
        }

        private static bool IsQuery(ScopeCommand cmd)
        {
            switch (cmd)
            {
                case ScopeCommand.Identify:
                case ScopeCommand.QueryActiveTrigger:
                case ScopeCommand.QueryTriggerMode:
                case ScopeCommand.QueryTriggerLevel:
                case ScopeCommand.QueryTimeDiv:
                case ScopeCommand.PopLastSystemError:
                case ScopeCommand.OperationComplete:
                    return true;
                default:
                    return false;
            }
        }

        protected virtual async Task ExecuteSuiteAsync(ScopeTestSuite suite, CancellationToken ct)
        {
            var steps = ScopeTestSuiteRegistry.Resolve(null, suite); // use defaults; UI layer can override via config
            foreach (var step in steps)
            {
                if (IsQuery(step))
                {
                    var scpi = Cmd(step);
                    await SendRawQueryAsync(scpi, ct).ConfigureAwait(false);
                }
                else if (step == ScopeCommand.DumpImage)
                {
                    // Not executed here; DumpImage is handled by CaptureScreenAsync or explicit calls
                    continue;
                }
                else
                {
                    var scpi = Cmd(step);
                    await SendRawWriteAsync(scpi, ct).ConfigureAwait(false);
                }
            }
        }

        protected virtual async Task<byte[]> ExecuteSuiteForDumpAsync(ScopeTestSuite suite, CancellationToken ct)
        {
            // Log suite header
            Logger.Instance.Debug("---");
            Logger.Instance.Debug(ScopeTestSuiteRegistry.GetDisplayName(suite) + ":");

            var steps = ScopeTestSuiteRegistry.Resolve(null, suite); // defaults
            byte[] data = null;
            foreach (var step in steps)
            {
                if (step == ScopeCommand.DumpImage)
                {
                    // Perform dump and read
                    string dumpCmd = Cmd(ScopeCommand.DumpImage);
                    Logger.Instance.Debug($"SCPI >> {dumpCmd}");
                    await Transport.WriteAsync(dumpCmd, ct).ConfigureAwait(false);
                    var raw = await ReadIeee4882BlockAsync(7000, 100, ct).ConfigureAwait(false);
                    Logger.Instance.Debug($"SCPI << <{raw?.Length ?? 0} bytes binary>");
                    try { await Transport.ReadBinaryAsync(50, ct).ConfigureAwait(false); } catch { }

                    // Log RAW first and last 64 bytes (unmodified) WITHOUT size suffix
                    try
                    {
                        Logger.Instance.Debug("Dumping FIRST 64 bytes from raw data stream:");
                        HexDump(raw, 0, Math.Min(64, raw?.Length ?? 0));
                    }
                    catch { }
                    try
                    {
                        Logger.Instance.Debug("Dumping LAST 64 bytes from raw data stream:");
                        var start = Math.Max(0, (raw?.Length ?? 0) - 64);
                        HexDump(raw, start, (raw?.Length ?? 0) - start);
                    }
                    catch { }

                    // Strip IEEE 488.2 block header if present (payload extraction)
                    data = raw;
                    if (data != null && data.Length >= 2 && data[0] == (byte)'#')
                    {
                        int nDigits = data[1] - (byte)'0';
                        if (nDigits >= 0 && nDigits <= 9 && data.Length >= 2 + nDigits)
                        {
                            int payloadLen = 0;
                            for (int i = 0; i < nDigits; i++)
                                payloadLen = (payloadLen * 10) + (data[2 + i] - (byte)'0');
                            int headerLen = 2 + nDigits;
                            if (payloadLen >= 0 && data.Length >= headerLen + payloadLen)
                            {
                                var payload = new byte[payloadLen];
                                Buffer.BlockCopy(data, headerLen, payload, 0, payloadLen);
                                data = payload;
                            }
                        }
                    }

                    // No payload head/tail logs per request
                }
                else if (IsQuery(step))
                {
                    var scpi = Cmd(step);
                    await SendRawQueryAsync(scpi, ct).ConfigureAwait(false);
                }
                else
                {
                    var scpi = Cmd(step);
                    await SendRawWriteAsync(scpi, ct).ConfigureAwait(false);
                }
            }
            return data ?? new byte[0];
        }

        private static void HexDump(byte[] data, int offset, int count)
        {
            if (data == null)
            {
                Logger.Instance.Debug("<null>");
                return;
            }

            int end = Math.Min(data.Length, offset + count);
            for (int i = offset; i < end; i += 16)
            {
                int lineLen = Math.Min(16, end - i);

                var hex = new StringBuilder(16 * 3 - 1);
                var ascii = new StringBuilder(16);

                for (int j = 0; j < 16; j++)
                {
                    if (j < lineLen)
                    {
                        byte b = data[i + j];
                        hex.Append(b.ToString("X2"));
                        ascii.Append(b >= 32 && b <= 126 ? (char)b : '.');
                    }
                    else
                    {
                        hex.Append("  ");
                    }
                    if (j != 15) hex.Append(' ');
                }

                Logger.Instance.Debug($"{i:X8}  {hex}   {ascii}");
            }
        }

        public virtual async Task StopAsync(CancellationToken ct = default(CancellationToken))
        {
            await ExecuteSuiteAsync(ScopeTestSuite.Stop, ct).ConfigureAwait(false);
        }

        public virtual async Task RunAsync(CancellationToken ct = default(CancellationToken))
        {
            await ExecuteSuiteAsync(ScopeTestSuite.Run, ct).ConfigureAwait(false);
        }

        public virtual async Task SingleAsync(CancellationToken ct = default(CancellationToken))
        {
            await ExecuteSuiteAsync(ScopeTestSuite.Single, ct).ConfigureAwait(false);
        }

        public virtual async Task<string> QueryActiveTriggerAsync(CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.QueryActiveTrigger);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            var resp = await Transport.QueryAsync(scpi, 5000, ct).ConfigureAwait(false);
            Logger.Instance.Debug($"SCPI << {Trunc(resp)}");
            return resp;
        }

        public virtual async Task<string> QueryTriggerModeAsync(CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.QueryTriggerMode);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            var resp = await Transport.QueryAsync(scpi, 5000, ct).ConfigureAwait(false);
            Logger.Instance.Debug($"SCPI << {Trunc(resp)}");
            return resp;
        }

        public virtual async Task<double> QueryTriggerLevelAsync(CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.QueryTriggerLevel);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            var s = await Transport.QueryAsync(scpi, 5000, ct).ConfigureAwait(false);
            Logger.Instance.Debug($"SCPI << {Trunc(s)}");
            if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v)) return v;
            throw new FormatException($"Unexpected trigger level '{s}'");
        }

        public virtual Task SetTriggerLevelAsync(double level, CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.SetTriggerLevel, level);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            return Transport.WriteAsync(scpi, ct);
        }

        public virtual async Task<double> QueryTimeDivAsync(CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.QueryTimeDiv);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            var s = await Transport.QueryAsync(scpi, 5000, ct).ConfigureAwait(false);
            Logger.Instance.Debug($"SCPI << {Trunc(s)}");
            if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v)) return v;
            throw new FormatException($"Unexpected time/div '{s}'");
        }

        public virtual Task SetTimeDivAsync(double seconds, CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.SetTimeDiv, seconds);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            return Transport.WriteAsync(scpi, ct);
        }

        public virtual Task ClearStatisticsAsync(CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.ClearStatistics);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            return Transport.WriteAsync(scpi, ct);
        }

        public virtual async Task<byte[]> DumpImageAsync(CancellationToken ct = default(CancellationToken))
        {
            return await Task.FromResult(new byte[0]);
        }

        // Reads an IEEE 488.2 block (#NLLLL...payload) using short per-read timeouts until full payload is received or deadline is reached
        private async Task<byte[]> ReadIeee4882BlockAsync(int totalDeadlineMs, int sliceTimeoutMs, CancellationToken ct)
        {
            var buf = new List<byte>(1200000);
            int headerLen = -1;
            int payloadLen = -1;
            var deadline = DateTime.UtcNow.AddMilliseconds(totalDeadlineMs);

            while (DateTime.UtcNow < deadline)
            {
                var chunk = await Transport.ReadBinaryAsync(sliceTimeoutMs, ct).ConfigureAwait(false);
                if (chunk != null && chunk.Length > 0)
                {
                    buf.AddRange(chunk);
                }

                if (headerLen < 0)
                {
                    if (buf.Count >= 2 && buf[0] == (byte)'#')
                    {
                        int nDigits = buf[1] - (byte)'0';
                        if (nDigits >= 0 && nDigits <= 9 && buf.Count >= 2 + nDigits)
                        {
                            headerLen = 2 + nDigits;
                            payloadLen = 0;
                            for (int i = 0; i < nDigits; i++)
                            {
                                payloadLen = (payloadLen * 10) + (buf[2 + i] - (byte)'0');
                            }
                        }
                    }
                }

                if (headerLen > 0 && payloadLen >= 0)
                {
                    if (buf.Count >= headerLen + payloadLen)
                    {
                        break;
                    }
                }
            }

            return buf.ToArray();
        }

        public virtual async Task<byte[]> CaptureScreenAsync(CancellationToken ct = default(CancellationToken))
        {
            // Drive via the DumpImage suite; this will only send the DumpImage step by default
            return await ExecuteSuiteForDumpAsync(ScopeTestSuite.DumpImage, ct).ConfigureAwait(false);
        }

        public virtual async Task<string> PopLastSystemErrorAsync(CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.PopLastSystemError);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            var resp = await Transport.QueryAsync(scpi, 5000, ct).ConfigureAwait(false);
            Logger.Instance.Debug($"SCPI << {Trunc(resp)}");
            return resp;
        }

        public virtual async Task<bool> QueryOperationCompleteAsync(CancellationToken ct = default(CancellationToken))
        {
            var scpi = Cmd(ScopeCommand.OperationComplete);
            Logger.Instance.Debug($"SCPI >> {scpi}");
            var s = await Transport.QueryAsync(scpi, 5000, ct).ConfigureAwait(false);
            Logger.Instance.Debug($"SCPI << {Trunc(s)}");
            return s.Trim() == "1";
        }

        public Task SendRawWriteAsync(string scpi, CancellationToken ct = default(CancellationToken))
        {
            Logger.Instance.Debug($"SCPI >> {scpi}");
            return Transport.WriteAsync(scpi, ct);
        }

        public async Task<string> SendRawQueryAsync(string scpi, CancellationToken ct = default(CancellationToken))
        {
            Logger.Instance.Debug($"SCPI >> {scpi}");
            var resp = await Transport.QueryAsync(scpi, 5000, ct).ConfigureAwait(false);
            Logger.Instance.Debug($"SCPI << {Trunc(resp)}");
            return resp;
        }

        public async Task<byte[]> SendRawDumpAndReadAsync(string scpi, CancellationToken ct = default(CancellationToken))
        {
            Logger.Instance.Debug($"SCPI >> {scpi}");
            await Transport.WriteAsync(scpi, ct).ConfigureAwait(false);
            var data = await ReadIeee4882BlockAsync(7000, 100, ct).ConfigureAwait(false);
            Logger.Instance.Debug($"SCPI << <{data?.Length ?? 0} bytes binary>");
            try { await Transport.ReadBinaryAsync(50, ct).ConfigureAwait(false); } catch { }

            // Add same hex-dump as ExecuteSuiteForDumpAsync
            try
            {
                Logger.Instance.Debug("Dumping FIRST 64 bytes from raw data stream:");
                HexDump(data, 0, Math.Min(64, data?.Length ?? 0));
            }
            catch { }
            try
            {
                Logger.Instance.Debug("Dumping LAST 64 bytes from raw data stream:");
                var start = Math.Max(0, (data?.Length ?? 0) - 64);
                HexDump(data, start, (data?.Length ?? 0) - start);
            }
            catch { }

            return data;
        }
    }
}
