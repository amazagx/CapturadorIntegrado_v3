using System;
using System.IO;
using System.Linq;

namespace CapturadorIntegrado
{
    public static class SyncModule_v3
    {
        public static void SaveSessionSummary(string outputDir, string prefix, string windowTitle, int count)
        {
            try
            {
                Directory.CreateDirectory(outputDir);
                string path = Path.Combine(outputDir, $"{Sanitize(prefix)}_session.txt");
                File.WriteAllText(path,
                    $"Ventana: {windowTitle}{Environment.NewLine}" +
                    $"Prefijo: {prefix}{Environment.NewLine}" +
                    $"Capturas: {count}{Environment.NewLine}" +
                    $"Fecha: {DateTime.Now}");
            }
            catch { }
        }

        private static string Sanitize(string input)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(input.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(safe) ? "captura" : safe;
        }
    }
}
