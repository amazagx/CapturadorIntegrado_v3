using System;
using System.IO;

namespace CapturadorIntegrado
{
    public static class Logger
    {
        /// <summary>
        /// Añade una línea al log con sello de tiempo [yyyy-MM-dd HH:mm:ss.fff].
        /// Crea la carpeta del log si no existe.
        /// Tolerante a errores: cualquier excepción se ignora.
        /// </summary>
        public static void Append(string logPath, string line)
        {
            try
            {
                // Asegurar carpeta
                var dir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                // Abrir en modo Append y permitir lectura concurrente
                using (var fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var sw = new StreamWriter(fs))
                {
                    var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    sw.WriteLine("[" + stamp + "] " + line);
                }
            }
            catch
            {
                // No queremos que un fallo de log tumbe la app
            }
        }
    }
}
