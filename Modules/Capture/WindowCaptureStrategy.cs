using System;
using System.Drawing;
using System.Drawing.Imaging;
using CapturadorIntegrado_v3.Core;

namespace CapturadorIntegrado_v3.Modules.Capture
{
    public sealed class WindowCaptureStrategy : ICaptureStrategy
    {
        public string Name => "Window";

        public Bitmap? Capture(SessionState s)
        {
            if (s.TargetHwnd == 0) return null;
            if (!Win32.GetWindowRect((IntPtr)s.TargetHwnd, out var rect)) return null;
            int width = Math.Max(1, rect.Right - rect.Left);
            int height = Math.Max(1, rect.Bottom - rect.Top);

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            IntPtr hdc = g.GetHdc();
            try
            {
                bool ok = Win32.PrintWindow((IntPtr)s.TargetHwnd, hdc, 0);
                g.ReleaseHdc(hdc);
                if (!ok)
                {
                    using var g2 = Graphics.FromImage(bmp);
                    g2.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }
                return bmp;
            }
            catch
            {
                try { g.ReleaseHdc(hdc); } catch { }
                bmp.Dispose();
                return null;
            }
        }
    }
}