using System;
using System.Runtime.InteropServices;
using CapturadorIntegrado_v3.Core;

namespace CapturadorIntegrado_v3.Modules.Playback
{
    public sealed class StarterModule_v4
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_K = 0x4B;
        private const byte VK_SPACE = 0x20;

        public bool StartPlayback(nint hwnd, ILogger log)
        {
            try
            {
                if (hwnd == 0) return false;
                Win32.SetForegroundWindow((IntPtr)hwnd);
                keybd_event(VK_K, 0, 0, UIntPtr.Zero);
                keybd_event(VK_K, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                keybd_event(VK_SPACE, 0, 0, UIntPtr.Zero);
                keybd_event(VK_SPACE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                log.Info("playback", "start", $"hwnd={(long)hwnd}");
                return true;
            }
            catch (Exception ex)
            {
                log.Warn("playback", "start_fail", ex.Message);
                return false;
            }
        }
    }
}