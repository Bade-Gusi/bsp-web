using System;
using System.Collections.ObjectModel;
using System.IO;

namespace BeiShuiCS2.Services
{
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception? ex = null);
        ObservableCollection<LogEntry> GetRecentLogs(int count = 100);
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class FileLogger : ILogger
    {
        private readonly ObservableCollection<LogEntry> _recentLogs = new();
        private readonly string _logDir;
        private readonly object _lock = new();

        public FileLogger()
        {
            _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            try { Directory.CreateDirectory(_logDir); } catch { }
        }

        public void Info(string message) => Write("INFO", message);
        public void Warn(string message) => Write("WARN", message);
        public void Error(string message, Exception? ex = null)
        {
            var msg = ex != null ? $"{message} | {ex.GetType().Name}: {ex.Message}" : message;
            Write("ERROR", msg);
        }

        private void Write(string level, string message)
        {
            var entry = new LogEntry { Level = level, Message = message };
            _recentLogs.Add(entry);
            if (_recentLogs.Count > 500)
                _recentLogs.RemoveAt(0);

            var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            System.Diagnostics.Debug.WriteLine(line);

            lock (_lock)
            {
                try
                {
                    var file = Path.Combine(_logDir, $"app_{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(file, line + Environment.NewLine);
                }
                catch { }
            }
        }

        public ObservableCollection<LogEntry> GetRecentLogs(int count = 100)
        {
            var result = new ObservableCollection<LogEntry>();
            int start = Math.Max(0, _recentLogs.Count - count);
            for (int i = start; i < _recentLogs.Count; i++)
                result.Add(_recentLogs[i]);
            return result;
        }
    }
}
