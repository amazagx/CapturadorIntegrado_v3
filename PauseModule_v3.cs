using System;
using System.Runtime.InteropServices;

namespace CapturadorIntegrado
{
    public static class PauseModule_v3
    {
        private const int WM_APPCOMMAND = 0x0319;
        private const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint SendMessage(nint hWnd, int Msg, nint wParam, nint lParam);

        public static void Toggle(nint hwnd)
        {
            if (hwnd == nint.Zero) return;
            nint lParam = (nint)(APPCOMMAND_MEDIA_PLAY_PAUSE << 16);
            SendMessage(hwnd, WM_APPCOMMAND, hwnd, lParam);
        }
    }
}
