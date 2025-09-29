using System;

namespace Oscilloscope_Network_Capture.Core.Logging
{
    public sealed class LogEventArgs : EventArgs
    {
        public DateTime TimestampUtc { get; }
        public LogLevel Level { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public LogEventArgs(LogLevel level, string message, Exception ex = null)
        {
            TimestampUtc = DateTime.UtcNow;
            Level = level;
            Message = message ?? string.Empty;
            Exception = ex;
        }

        private static string Innermost(Exception ex)
        {
            if (ex == null) return string.Empty;
            while (ex.InnerException != null) ex = ex.InnerException;
            return ex.Message ?? ex.ToString();
        }

        public override string ToString()
        {
            var ts = TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
            if (Exception != null)
                return $"[{ts}] [{Level}] {Message} | EX: {Innermost(Exception)}";
            return $"[{ts}] [{Level}] {Message}";
        }
    }
}
