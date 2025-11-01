using System;
using System.Windows;
using System.Windows.Interop;
using CapturadorIntegrado_v3.Core;

namespace CapturadorIntegrado_v3.Hotkeys
{
    public sealed class HotkeyManager : IDisposable
    {
        private readonly Window _window;
        private readonly FeatureFlags _flags;
        private HwndSource? _source;

        public event Action? StartRequested;
        public event Action? CaptureRequested;
        public event Action? TogglePauseRequested;

        public HotkeyManager(Window window, FeatureFlags flags)
        {
            _window = window;
            _flags = flags;
            _window.Loaded += OnLoaded;
            _window.Closed += (_, __) => Dispose();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(_window);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);

            Win32.RegisterHotKey(helper.Handle, 1, _flags.HotkeyModifierMask, 0x31); // Ctrl+Alt+1
            Win32.RegisterHotKey(helper.Handle, 2, _flags.HotkeyModifierMask, 0x32); // Ctrl+Alt+2
            Win32.RegisterHotKey(helper.Handle, 3, _flags.HotkeyModifierMask, 0x33); // Ctrl+Alt+3
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                switch (id)
                {
                    case 1: StartRequested?.Invoke(); handled = true; break;
                    case 2: CaptureRequested?.Invoke(); handled = true; break;
                    case 3: TogglePauseRequested?.Invoke(); handled = true; break;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(_window);
                var h = helper.Handle;
                Win32.UnregisterHotKey(h, 1);
                Win32.UnregisterHotKey(h, 2);
                Win32.UnregisterHotKey(h, 3);
            }
            catch { }
            try { _source?.RemoveHook(HwndHook); } catch { }
        }
    }
}