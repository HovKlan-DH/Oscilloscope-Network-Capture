using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Oscilloscope_Network_Capture.Core.Logging
{
    public sealed class Logger
    {
        private readonly object _sync = new object();
        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly StringBuilder _memoryLog = new StringBuilder();

        public static Logger Instance { get; } = new Logger();

        public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

        public event EventHandler<LogEventArgs> MessageLogged;

        private Logger()
        {
            // Place a single logfile alongside the executable and overwrite it on each launch
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _logDirectory = baseDir;
            Directory.CreateDirectory(_logDirectory);
            var fileName = "Oscilloscope Network Capture.log";
            _logFilePath = Path.Combine(_logDirectory, fileName);

            // Truncate/overwrite on startup
            try { using (var fs = File.Create(_logFilePath)) { } } catch { }
        }

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message)  => Log(LogLevel.Info,  message);
//        public void Warn(string message)  => Log(LogLevel.Warn,  message);
        public void Error(string message, Exception ex = null) => Log(LogLevel.Error, message, ex);

        public void Log(LogLevel level, string message, Exception ex = null)
        {
            if (level < MinimumLevel) return;

            var args = new LogEventArgs(level, message, ex);

            try
            {
                var line = args.ToString() + Environment.NewLine;
                lock (_sync)
                {
                    File.AppendAllText(_logFilePath, line, Encoding.UTF8);
                    _memoryLog.Append(line);
                }
            }
            catch
            {
            }

            try
            {
                MessageLogged?.Invoke(this, args);
            }
            catch
            {
            }
        }

        // File-only logging utilities (now mirrored to UI by using Log())
        public void FileOnly(string message)
        {
            // Use Log to keep UI and file in sync
            Log(LogLevel.Debug, message);
        }

        public void FileOnlyBinary(string title, byte[] data, int maxBytes = 512)
        {
            if (data == null) { FileOnly(title + " <null>"); return; }
            if (maxBytes < 0) maxBytes = 0;
            int toDump = Math.Min(maxBytes, data.Length);
            Log(LogLevel.Debug, $"{title} <{data.Length} bytes, showing {toDump} head bytes>");
            DumpBinaryRange(data, 0, toDump);
        }

        public void FileOnlyBinaryHeadTail(string title, byte[] data, int headBytes = 128, int tailBytes = 128)
        {
            if (data == null) { FileOnly(title + " <null>"); return; }
            if (headBytes < 0) headBytes = 0; if (tailBytes < 0) tailBytes = 0;
            int len = data.Length;
            int head = Math.Min(headBytes, len);
            int tail = Math.Min(tailBytes, Math.Max(0, len - head));

            Log(LogLevel.Debug, $"{title} <{len} bytes in total>");
            Log(LogLevel.Debug, $"Dumping first {head} bytes:");

            if (head > 0)
                DumpBinaryRange(data, 0, head);

            Log(LogLevel.Debug, $"Dumping last {tail} bytes:");
            if (tail > 0)
            {
                int start = len - tail;
                int count = len - start;
                if (count > 0)
                {
                    DumpBinaryRange(data, start, count);
                }
            }
        }

        public void FileOnlyBinaryTail(string title, byte[] data, int tailBytes = 64)
        {
            if (data == null) { FileOnly(title + " <null>"); return; }
            if (tailBytes < 0) tailBytes = 0;
            int len = data.Length;
            int count = Math.Min(tailBytes, len);
            int start = Math.Max(0, len - count);
            Log(LogLevel.Debug, $"{title} <{len} bytes, showing {count} tail bytes>");
            if (count > 0)
            {
                DumpBinaryRange(data, start, count);
            }
        }

        private void DumpBinaryRange(byte[] data, int offset, int count)
        {
            int end = offset + count;
            for (int i = offset; i < end; i += 16)
            {
                int n = Math.Min(16, end - i);
                var hex = new StringBuilder(16 * 3 + 1);
                var ascii = new StringBuilder(16);
                for (int j = 0; j < n; j++)
                {
                    byte b = data[i + j];
                    hex.Append(b.ToString("X2"));
                    if (j != n - 1) hex.Append(' ');
                    ascii.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }
                Log(LogLevel.Debug, string.Format("{0}  {1,-48}  {2}", i.ToString("X8"), hex.ToString(), ascii.ToString()));
            }
        }

        // Extra helpers used by the UI
        public void ClearLog()
        {
            lock (_sync)
            {
                try { using (var fs = File.Create(_logFilePath)) { } } catch { }
                _memoryLog.Clear();
            }
        }

        public void SaveLog(string filePath)
        {
            lock (_sync)
            {
                File.WriteAllText(filePath, _memoryLog.ToString(), Encoding.UTF8);
            }
        }

        public string GetLogForClipboard()
        {
            lock (_sync)
            {
                return _memoryLog.ToString();
            }
        }
    }
}
