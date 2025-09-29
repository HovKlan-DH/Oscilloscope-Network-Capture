using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Oscilloscope_Network_Capture.Core.Transport
{
    // Simple TCP-based transport for SCPI over socket (e.g., :PORT 5025)
    // Resource format: host:port (e.g., 192.168.1.100:5025)
    public sealed class SocketInstrumentTransport : IInstrumentTransport
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public bool IsConnected
        {
            get
            {
                try
                {
                    return _client != null
                        && _client.Client != null
                        && _client.Client.Connected
                        && _stream != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task ConnectAsync(string resource, int timeoutMs, CancellationToken ct = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentException("Resource must be host:port");
            var parts = resource.Split(':');
            if (parts.Length != 2) throw new ArgumentException("Resource must be in form host:port");
            string host = parts[0];
            if (!int.TryParse(parts[1], out int port)) throw new ArgumentException("Invalid port in resource");

            var client = new TcpClient();
            try
            {
                var connectTask = client.ConnectAsync(host, port);
                Task timeoutTask = timeoutMs > 0 ? Task.Delay(timeoutMs, ct) : Task.CompletedTask;
                var completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                if (completed != connectTask)
                {
                    // Canceled or timed out
                    try { client.Close(); } catch { }
                    if (ct.IsCancellationRequested)
                        throw new OperationCanceledException(ct);
                    throw new TimeoutException("Timeout connecting to instrument.");
                }

                // Propagate any connect exception
                await connectTask.ConfigureAwait(false);

                var stream = client.GetStream();
                stream.ReadTimeout = timeoutMs;
                stream.WriteTimeout = timeoutMs;

                // Success: assign fields
                _client = client;
                _stream = stream;
            }
            catch
            {
                try { client.Close(); } catch { }
                _stream = null;
                _client = null;
                throw;
            }
        }

        public Task DisconnectAsync()
        {
            try
            {
                _stream?.Dispose();
            }
            catch { /* ignore */ }
            try
            {
                _client?.Close();
            }
            catch { /* ignore */ }
            _stream = null;
            _client = null;
            return Task.CompletedTask;
        }

        public async Task WriteAsync(string command, CancellationToken ct = default(CancellationToken))
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected.");
            var data = Encoding.ASCII.GetBytes(command + "\n");
            await _stream.WriteAsync(data, 0, data.Length, ct).ConfigureAwait(false);
            await _stream.FlushAsync(ct).ConfigureAwait(false);
        }

        public async Task<string> QueryAsync(string command, int readTimeoutMs, CancellationToken ct = default(CancellationToken))
        {
            await WriteAsync(command, ct).ConfigureAwait(false);
            return await ReadLineAsync(readTimeoutMs, ct).ConfigureAwait(false);
        }

        private async Task<string> ReadLineAsync(int readTimeoutMs, CancellationToken ct)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected.");
            using (var ms = new MemoryStream())
            {
                var buffer = new byte[1];
                var start = DateTime.UtcNow;
                while (true)
                {
                    if (readTimeoutMs > 0 && (DateTime.UtcNow - start).TotalMilliseconds > readTimeoutMs)
                        throw new TimeoutException("Read timeout.");
                    if (_stream.DataAvailable)
                    {
                        int read = await _stream.ReadAsync(buffer, 0, 1, ct).ConfigureAwait(false);
                        if (read <= 0) break;
                        if (buffer[0] == (byte)'\n') break;
                        ms.WriteByte(buffer[0]);
                    }
                    else
                    {
                        await Task.Delay(2, ct).ConfigureAwait(false);
                    }
                }
                return Encoding.ASCII.GetString(ms.ToArray()).TrimEnd('\r');
            }
        }

        public async Task<byte[]> ReadBinaryAsync(int readTimeoutMs, CancellationToken ct = default(CancellationToken))
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected.");
            using (var ms = new MemoryStream())
            {
                var start = DateTime.UtcNow;
                var buffer = new byte[8192];
                while (true)
                {
                    if (readTimeoutMs > 0 && (DateTime.UtcNow - start).TotalMilliseconds > readTimeoutMs)
                        break;
                    if (_stream.DataAvailable)
                    {
                        int read = await _stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                        if (read <= 0) break;
                        ms.Write(buffer, 0, read);
                        start = DateTime.UtcNow; // extend timeout while data flows
                    }
                    else
                    {
                        await Task.Delay(2, ct).ConfigureAwait(false);
                    }
                }
                return ms.ToArray();
            }
        }

        public async Task WriteBinaryAsync(byte[] data, CancellationToken ct = default(CancellationToken))
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected.");
            await _stream.WriteAsync(data, 0, data.Length, ct).ConfigureAwait(false);
            await _stream.FlushAsync(ct).ConfigureAwait(false);
        }
    }
}
