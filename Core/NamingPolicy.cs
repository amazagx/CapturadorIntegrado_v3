using System;
using System.IO;

namespace CapturadorIntegrado_v3.Core
{
    public sealed class NamingPolicy
    {
        public string BuildFileName(string prefix, int counter, TimeSpan elapsed)
        {
            string safe = Sanitize(prefix);
            return $"{safe}_{counter:0000}_+{elapsed.Minutes:00}m{elapsed.Seconds:00}s.png";
        }

        private static string Sanitize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "capturas";
            foreach (var c in Path.GetInvalidFileNameChars())
                raw = raw.Replace(c, '_');
            return raw.Trim();
        }
    }
}