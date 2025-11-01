using System;
using System.IO;
using System.Text;

namespace CapturadorIntegrado_v3.Core
{
    public interface ILogger
    {
        void Info(string module, string action, string? extra = null);
        void Warn(string module, string action, string? extra = null);
        void Error(string module, string action, string? extra = null);
        event Action<string>? LineEmitted; // UI can subscribe
    }

    public sealed class StructuredLogger : ILogger, IDisposable
    {
        private readonly IClock _clock;
        private readonly string? _filePath;
        private StreamWriter? _writer;

        public StructuredLogger(IClock clock, string? filePath = null)
        {
            _clock = clock;
            _filePath = filePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                _writer = new StreamWriter(File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8);
                _writer.AutoFlush = true;
            }
        }

        public event Action<string>? LineEmitted;

        private void Emit(string level, string module, string action, string? extra)
        {
            string ts = _clock.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string line = $"{ts} [{module}] {level} {action}" + (string.IsNullOrWhiteSpace(extra) ? "" : $"  {extra}");
            _writer?.WriteLine(line);
            LineEmitted?.Invoke(line);
        }

        public void Info(string module, string action, string? extra = null) => Emit("info", module, action, extra);
        public void Warn(string module, string action, string? extra = null) => Emit("warn", module, action, extra);
        public void Error(string module, string action, string? extra = null) => Emit("error", module, action, extra);

        public void Dispose()
        {
            try { _writer?.Dispose(); } catch { }
        }
    }
}