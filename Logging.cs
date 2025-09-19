using System;

namespace Oscilloscope_Network_Capture
{
    public enum LogLevel { Debug, Info, Notice, Highlight, Warning, Error }

    public sealed class LogEntry
    {
        public DateTime Timestamp { get; }
        public string Message { get; }
        public LogLevel Level { get; }

        public LogEntry(string message, LogLevel level)
        {
            Timestamp = DateTime.Now;
            Message = message ?? "";
            Level = level;
        }
    }

    public sealed class Logger
    {
        public event Action<LogEntry> Message;

        public void Log(string message, LogLevel level = LogLevel.Info)
            => Message?.Invoke(new LogEntry(message, level));

        public void Debug(string message) => Log(message, LogLevel.Debug);
        public void Info(string message) => Log(message, LogLevel.Info);
        public void Notice(string message) => Log(message, LogLevel.Notice);
        public void Highlight(string message) => Log(message, LogLevel.Highlight);
        public void Warn(string message) => Log(message, LogLevel.Warning);
        public void Error(string message) => Log(message, LogLevel.Error);
    }
}