using System.Threading;
using System.Threading.Tasks;

namespace Oscilloscope_Network_Capture.Core.Transport
{
    public interface IInstrumentTransport
    {
        Task ConnectAsync(string resource, int timeoutMs, CancellationToken ct = default(CancellationToken));
        Task DisconnectAsync();

        bool IsConnected { get; }

        Task WriteAsync(string command, CancellationToken ct = default(CancellationToken));
        Task<string> QueryAsync(string command, int readTimeoutMs, CancellationToken ct = default(CancellationToken));

        Task<byte[]> ReadBinaryAsync(int readTimeoutMs, CancellationToken ct = default(CancellationToken));
        Task WriteBinaryAsync(byte[] data, CancellationToken ct = default(CancellationToken));
    }
}
