using CapturadorIntegrado;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace CapturadorIntegrado_v3
{
    public partial class MainWindow : Window
    {
        // --- Win32 helpers to resolve a window by title substring ---
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private static string GetWindowTitle(IntPtr h)
        {
            var sb = new StringBuilder(1024);
            _ = GetWindowTextW(h, sb, sb.Capacity);
            return sb.ToString();
        }

        private static IntPtr FindFirstWindowByTitleSubstring(string needle)
        {
            IntPtr found = IntPtr.Zero;
            if (string.IsNullOrWhiteSpace(needle)) return found;

            string norm = needle.Trim();
            EnumWindows((h, l) =>
            {
                if (!IsWindowVisible(h)) return true;
                string title = GetWindowTitle(h);
                if (!string.IsNullOrEmpty(title) && title.IndexOf(norm, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = h;
                    return false; // stop
                }
                return true; // continue
            }, IntPtr.Zero);

            return found;
        }

        // --- Session state ---
        private IntPtr _targetHwnd = IntPtr.Zero;
        private string _targetTitle = string.Empty;
        private DateTime _sessionStartUtc;
        private bool _sessionActive = false;
        private int _counter = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Carpeta por defecto (si no hay nada)
            if (string.IsNullOrWhiteSpace(TbDest.Text))
                TbDest.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            // Escalas básicas
            if (CmbScale.Items.Count == 0)
            {
                CmbScale.Items.Add("100 %");
                CmbScale.Items.Add("125 %");
                CmbScale.Items.Add("150 %");
                CmbScale.Items.Add("200 %");
                CmbScale.SelectedIndex = 0;
            }

            UpdateStatus("Inactivo");

            // Atajos de teclado (sobre esta ventana)
            this.PreviewKeyDown += Window_PreviewKeyDown;
        }

        // --- UI helpers ---
        private void UpdateStatus(string text) => LblStatus.Content = text;

        private void AppendLog(string line)
        {
            TbLog.AppendText(line + Environment.NewLine);
            TbLog.ScrollToEnd();
        }

        private static string SanitizePrefix(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "capturas";
            foreach (var c in Path.GetInvalidFileNameChars()) raw = raw.Replace(c, '_');
            return raw.Trim();
        }

        private static double ParseScaleFactor(string item)
        {
            if (string.IsNullOrEmpty(item)) return 1.0;
            var digits = new string(item.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int pct) && pct > 0) return pct / 100.0;
            return 1.0;
        }

        private string BuildCapturePath(TimeSpan elapsed)
        {
            string prefix = SanitizePrefix(TbPrefix.Text);
            string folder = TbDest.Text;
            Directory.CreateDirectory(folder);
            string name = $"{prefix}_{_counter:0000}_+{elapsed.Minutes:00}m{elapsed.Seconds:00}s.png";
            return Path.Combine(folder, name);
        }

        private TimeSpan EffectiveElapsed() => DateTime.UtcNow - _sessionStartUtc;

        private void PushThumbnail(string filePath)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(filePath);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 280;
                bmp.EndInit();

                var img = new System.Windows.Controls.Image
                {
                    Source = bmp,
                    Height = 120,
                    Margin = new Thickness(2)
                };

                var lbi = new ListBoxItem { Content = img, Tag = filePath };
                LbThumbs.Items.Insert(0, lbi); // última primero
                while (LbThumbs.Items.Count > 24) LbThumbs.Items.RemoveAt(LbThumbs.Items.Count - 1);
            }
            catch { /* no crítico */ }
        }

        // --- Handlers de botones ---
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new Forms.FolderBrowserDialog())
            {
                dlg.Description = "Elige la carpeta donde guardar las capturas";
                dlg.SelectedPath = Directory.Exists(TbDest.Text)
                    ? TbDest.Text
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                var result = dlg.ShowDialog();
                if (result == Forms.DialogResult.OK && Directory.Exists(dlg.SelectedPath))
                    TbDest.Text = dlg.SelectedPath;
            }
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            _targetTitle = TbTarget.Text?.Trim() ?? string.Empty;
            _targetHwnd = FindFirstWindowByTitleSubstring(_targetTitle);
            if (_targetHwnd != IntPtr.Zero)
                AppendLog($"Ventana detectada: '{GetWindowTitle(_targetHwnd)}'");
            else
                AppendLog("No se encontró ninguna ventana que coincida.");
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionActive)
            {
                AppendLog("Ya hay una sesión activa.");
                return;
            }

            if (_targetHwnd == IntPtr.Zero)
                BtnBuscar_Click(sender, e);

            if (_targetHwnd == IntPtr.Zero)
            {
                AppendLog("No se puede iniciar: ventana no localizada.");
                return;
            }

            // Preparar sesión
            _counter = 0;
            _sessionStartUtc = DateTime.UtcNow;
            _sessionActive = true;

            // Intentar arrancar (PLAY) sobre la ventana objetivo
            StarterModule_v3.StartPlayback((nint)_targetHwnd);

            UpdateStatus("Activo");
            AppendLog($"Sesión iniciada. Ventana='{GetWindowTitle(_targetHwnd)}' Prefijo='{SanitizePrefix(TbPrefix.Text)}' - Escala={CmbScale.Text}");
            SetButtonsEnabled(running: true);
        }

        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionActive || _targetHwnd == IntPtr.Zero)
            {
                AppendLog("No hay sesión activa o ventana objetivo.");
                return;
            }

            var elapsed = EffectiveElapsed();
            string path = BuildCapturePath(elapsed);
            double scaleFactor = ParseScaleFactor(CmbScale.Text);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                // Usa el módulo de captura existente (namespace CapturadorIntegrado)
                CaptureModule_v3.CaptureActiveWindowToFile((nint)_targetHwnd, scaleFactor, path);
                _counter++;
                AppendLog($"Captura guardada: {Path.GetFileName(path)}");
                PushThumbnail(path);
            }
            catch (Exception ex)
            {
                AppendLog($"Error al capturar: {ex.Message}");
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionActive || _targetHwnd == IntPtr.Zero) return;

            try
            {
                PauseModule_v3.Toggle((nint)_targetHwnd);
                AppendLog("Pausa/Reanudar enviado");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR Pausa/Reanudar] {ex.Message}");
            }
        }

        private void BtnEnd_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionActive)
            {
                // Aun así, limpia UI de rastros de sesiones anteriores
                TbLog.Clear();
                LbThumbs.Items.Clear();
                return;
            }

            _sessionActive = false;
            UpdateStatus("Finalizado");
            AppendLog($"Sesión finalizada. Total capturas={_counter}");

            try
            {
                SyncModule_v3.SaveSessionSummary(
                    TbDest.Text,
                    SanitizePrefix(TbPrefix.Text),
                    _targetTitle,
                    _counter
                );
            }
            catch { /* no crítico */ }

            // Reset de UI (conservando Destino y Escala)
            TbTarget.Text = "";
            TbPrefix.Text = "";
            _targetHwnd = IntPtr.Zero;
            _targetTitle = "";
            _counter = 0;

            // Limpia log y miniaturas
            TbLog.Clear();
            LbThumbs.Items.Clear();

            SetButtonsEnabled(running: false);
        }

        private void SetButtonsEnabled(bool running)
        {
            BtnStart.IsEnabled = !running;
            BtnCapture.IsEnabled = running;
            BtnPauseResume.IsEnabled = running;
            BtnEnd.IsEnabled = running;
        }

        // --- Atajos de teclado (F11/F12/F10) ---
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F11) { BtnStart_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.F12) { BtnCapture_Click(sender, e); e.Handled = true; }
            else if (e.Key == Key.F10) { BtnPause_Click(sender, e); e.Handled = true; }
        }

        // --- Doble click en miniatura: abrir la imagen con el visor predeterminado ---
        private void OnThumbDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // Soportar tanto ListBoxItem (con Tag=path) como Image con BitmapImage.UriSource
                if (LbThumbs.SelectedItem is ListBoxItem item && item.Tag is string fullPath && File.Exists(fullPath))
                {
                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
                else if (LbThumbs.SelectedItem is System.Windows.Controls.Image img
                         && img.Source is BitmapImage bmp
                         && bmp.UriSource != null)
                {
                    var path = bmp.UriSource.LocalPath;
                    if (File.Exists(path))
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[WARN Abrir captura] {ex.Message}");
            }
        }
    }
}
