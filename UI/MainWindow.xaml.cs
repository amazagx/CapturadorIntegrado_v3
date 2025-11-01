using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CapturadorIntegrado_v3.Core;
using CapturadorIntegrado_v3.Hotkeys;
using CapturadorIntegrado_v3.Modules.Capture;
using CapturadorIntegrado_v3.Modules.Playback;
using CapturadorIntegrado_v3.Modules.Sync;
using Forms = System.Windows.Forms;

namespace CapturadorIntegrado_v3
{
    public partial class MainWindow : Window
    {
        private readonly SystemClock _clock = new();
        private readonly FeatureFlags _flags = new();
        private StructuredLogger? _logger;
        private readonly NamingPolicy _naming = new();
        private readonly SessionState _state = new();
        private HotkeyManager? _hotkeys;
        private ISessionCommands? _commands;
        private CaptureModule_v4? _capture;

        public MainWindow()
        {
            InitializeComponent();
            InitUiDefaults();
            InitCore();
        }

        private void InitUiDefaults()
        {
            if (string.IsNullOrWhiteSpace(TbDest.Text))
            {
                string defaultPath = @"C:\\Users\\amaza\\Documents\\Destino Capturas";
                TbDest.Text = Directory.Exists(defaultPath)
                    ? defaultPath
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }

            if (CmbScale.Items.Count == 0)
            {
                CmbScale.Items.Add("100 %");
                CmbScale.Items.Add("125 %");
                CmbScale.Items.Add("150 %");
                CmbScale.Items.Add("200 %");
                CmbScale.SelectedIndex = 0;
            }

            CmbScreen.Items.Clear();
            CmbScreen.Items.Add("Auto");
            var screens = Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                CmbScreen.Items.Add($"{i + 1} - {s.Bounds.Width}×{s.Bounds.Height}" + (s.Primary ? " (Primaria)" : ""));
            }
            CmbScreen.SelectedIndex = 0;

            UpdateStatus("Inactivo");
            SetButtonsEnabled(false);
        }

        private void InitCore()
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Capturador", "capturador_log.txt");
            _logger = new StructuredLogger(_clock, logPath);
            _logger.LineEmitted += AppendLog;

            var windowStrategy = new WindowCaptureStrategy();
            var screenStrategy = new ScreenCaptureStrategy();

            _capture = new CaptureModule_v4(windowStrategy, screenStrategy, _flags, _naming, _clock, _logger);
            _capture.CaptureSaved += PushThumbnail;

            var starter = new StarterModule_v4();
            var pause = new PauseModule_v4();
            var sync = new SyncModule_v4(_clock, _logger);

            _commands = new SessionCommands(_state, starter, pause, _capture, sync, _clock, _logger, _flags);

            _hotkeys = new HotkeyManager(this, _flags);
            _hotkeys.StartRequested += () => BtnStart_Click(this, new RoutedEventArgs());
            _hotkeys.CaptureRequested += () => BtnCapture_Click(this, new RoutedEventArgs());
            _hotkeys.TogglePauseRequested += () => BtnPause_Click(this, new RoutedEventArgs());

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.F11) { BtnStart_Click(s, e); e.Handled = true; }
                else if (e.Key == Key.F12) { BtnCapture_Click(s, e); e.Handled = true; }
                else if (e.Key == Key.F10) { BtnPause_Click(s, e); e.Handled = true; }
            };
        }

        private void UpdateStatus(string text) => LblStatus.Text = text;

        private void AppendLog(string line)
        {
            Dispatcher.Invoke(() =>
            {
                TbLog.AppendText(line + Environment.NewLine);
                TbLog.ScrollToEnd();
            });
        }

        private static double ParseScaleFactor(string item)
        {
            if (string.IsNullOrEmpty(item)) return 1.0;
            var digits = new string(item.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int pct) && pct > 0) return pct / 100.0;
            return 1.0;
        }

        private int? ReadScreenIndex()
        {
            if (CmbScreen.SelectedIndex <= 0) return null;
            return CmbScreen.SelectedIndex - 1;
        }

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
                LbThumbs.Items.Insert(0, lbi);
                while (LbThumbs.Items.Count > 24) LbThumbs.Items.RemoveAt(LbThumbs.Items.Count - 1);
            }
            catch { }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new Forms.FolderBrowserDialog();
            dlg.Description = "Elige la carpeta donde guardar las capturas";
            dlg.SelectedPath = Directory.Exists(TbDest.Text)
                ? TbDest.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (dlg.ShowDialog() == Forms.DialogResult.OK && Directory.Exists(dlg.SelectedPath))
                TbDest.Text = dlg.SelectedPath;
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            string needle = TbTarget.Text?.Trim() ?? "";
            var hwnd = CapturadorIntegrado_v3.Core.Win32.FindFirstWindowByTitleSubstring(needle);
            if (hwnd != IntPtr.Zero)
                AppendLog($"Ventana detectada: '{CapturadorIntegrado_v3.Core.Win32.GetWindowTitle(hwnd)}'");
            else
                AppendLog("No se encontró ninguna ventana que coincida.");
        }

        private void BtnProbe_Click(object sender, RoutedEventArgs e)
        {
            if (_commands == null) return;
            var ok = _commands.ProbeTarget(
                TbTarget.Text?.Trim() ?? "",
                ReadScreenIndex(),
                TbDest.Text,
                TbPrefix.Text?.Trim() ?? "capturas",
                ParseScaleFactor(CmbScale.Text),
                out string? samplePath,
                out string? msg
            );
            if (ok && !string.IsNullOrEmpty(samplePath) && File.Exists(samplePath))
            {
                AppendLog($"[PROBE] Muestra: {Path.GetFileName(samplePath)}");
                PushThumbnail(samplePath);
                System.Windows.MessageBox.Show(this, "Objetivo verificado. ¿Arrancamos la sesión?", "Verificación OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show(this, msg ?? "Fallo de verificación.", "Verificación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_commands == null) return;
            bool ok = _commands.Start(
                TbTarget.Text?.Trim() ?? "",
                TbDest.Text,
                TbPrefix.Text?.Trim() ?? "capturas",
                ParseScaleFactor(CmbScale.Text),
                ReadScreenIndex()
            );
            if (ok) { UpdateStatus("Activo"); AppendLog("Sesión iniciada."); SetButtonsEnabled(true); }
            else AppendLog("No se pudo iniciar la sesión.");
        }

        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            if (_commands == null) return;
            if (!_commands.CaptureOnce()) AppendLog("Error al capturar.");
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (_commands == null) return;
            _commands.TogglePause();
        }

        private void BtnEnd_Click(object sender, RoutedEventArgs e)
        {
            if (_commands == null) return;
            _commands.End();
            TbLog.Clear();
            LbThumbs.Items.Clear();
            UpdateStatus("Finalizado");
            SetButtonsEnabled(false);
        }

        private void SetButtonsEnabled(bool running)
        {
            BtnStart.IsEnabled = !running;
            BtnCapture.IsEnabled = running;
            BtnPauseResume.IsEnabled = running;
            BtnEnd.IsEnabled = running;
        }

        private void OnThumbDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (LbThumbs.SelectedItem is ListBoxItem item &&
                    item.Tag is string fullPath &&
                    File.Exists(fullPath))
                {
                    var psi = new ProcessStartInfo(fullPath) { UseShellExecute = true };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[WARN Abrir captura] {ex.Message}");
            }
        }
    }
}
