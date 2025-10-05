using System.Threading;
using System.Threading.Tasks;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public interface IScope
    {
        string Vendor { get; }
        string Model { get; }
        string Resource { get; set; }

        Task ConnectAsync(CancellationToken ct = default(CancellationToken));
        Task DisconnectAsync();
        bool IsConnected { get; }

        // Identification
        Task<string> IdentifyAsync(CancellationToken ct = default(CancellationToken));

        // Acquisition control
        Task StopAsync(CancellationToken ct = default(CancellationToken));
        Task RunAsync(CancellationToken ct = default(CancellationToken));
        Task SingleAsync(CancellationToken ct = default(CancellationToken));

        // Trigger
        Task<string> QueryActiveTriggerAsync(CancellationToken ct = default(CancellationToken));
        Task<string> QueryTriggerModeAsync(CancellationToken ct = default(CancellationToken));
        Task<double> QueryTriggerLevelAsync(CancellationToken ct = default(CancellationToken));
        Task SetTriggerLevelAsync(double level, CancellationToken ct = default(CancellationToken));

        // Time base
        Task<double> QueryTimeDivAsync(CancellationToken ct = default(CancellationToken));
        Task SetTimeDivAsync(double seconds, CancellationToken ct = default(CancellationToken));

        // Statistics
        Task ClearStatisticsAsync(CancellationToken ct = default(CancellationToken));

        // Image / screenshot
        Task<byte[]> DumpImageAsync(CancellationToken ct = default(CancellationToken));
        Task<byte[]> CaptureScreenAsync(CancellationToken ct = default(CancellationToken));

        // System
        Task<string> DrainSystemErrorQueueAsync(CancellationToken ct = default(CancellationToken));
        Task<bool> QueryOperationCompleteAsync(CancellationToken ct = default(CancellationToken));

        // Raw
        Task SendRawWriteAsync(string scpi, CancellationToken ct = default(CancellationToken));
        Task<string> SendRawQueryAsync(string scpi, CancellationToken ct = default(CancellationToken));
        Task<byte[]> SendRawDumpAndReadAsync(string scpi, CancellationToken ct = default(CancellationToken));
    }
}
