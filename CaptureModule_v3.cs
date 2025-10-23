using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CapturadorIntegrado_v3
{
    public static class CaptureModule_v3
    {
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern nint GetWindowDC(nint hWnd);

        [DllImport("gdi32.dll")]
        private static extern int BitBlt(nint hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            nint hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern nint CreateCompatibleDC(nint hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(nint hdc);

        [DllImport("gdi32.dll")]
        private static extern nint SelectObject(nint hdc, nint h);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(nint ho);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(nint hWnd, nint hDc);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int SRCCOPY = 0x00CC0020;

        /// <summary>
        /// Captura el contenido **pintado** de la ventana (no el escritorio) a un fichero.
        /// </summary>
        /// <param name="hwnd">handle de la ventana destino</param>
        /// <param name="scalePercent">escala 100, 90, 80…</param>
        /// <param name="fullPath">ruta completa del PNG de salida</param>
        public static string CaptureActiveWindowToFile(nint hwnd, double scalePercent, string fullPath)
        {
            if (hwnd == 0)
                throw new ArgumentException("hwnd inválido");

            if (!GetWindowRect(hwnd, out var rect))
                throw new InvalidOperationException("GetWindowRect falló");

            int w = Math.Max(1, rect.Right - rect.Left);
            int h = Math.Max(1, rect.Bottom - rect.Top);

            // DC de la ventana
            nint hdcSrc = GetWindowDC(hwnd);
            if (hdcSrc == nint.Zero)
                throw new InvalidOperationException("GetWindowDC falló");

            // DC compatible (destino)
            nint hdcDest = CreateCompatibleDC(hdcSrc);
            if (hdcDest == nint.Zero)
            {
                ReleaseDC(hwnd, hdcSrc);
                throw new InvalidOperationException("CreateCompatibleDC falló");
            }

            // Bitmap GDI
            using var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var hBmp = bmp.GetHbitmap();

            try
            {
                nint old = SelectObject(hdcDest, hBmp);
                _ = BitBlt(hdcDest, 0, 0, w, h, hdcSrc, 0, 0, SRCCOPY);
                SelectObject(hdcDest, old);
            }
            finally
            {
                DeleteDC(hdcDest);
                ReleaseDC(hwnd, hdcSrc);
            }

            // Reescalado (si procede) y guardado
            Bitmap toSave = bmp;
            try
            {
                if (scalePercent > 0 && Math.Abs(scalePercent - 100.0) > double.Epsilon)
                {
                    int sw = Math.Max(1, (int)Math.Round(w * (scalePercent / 100.0)));
                    int sh = Math.Max(1, (int)Math.Round(h * (scalePercent / 100.0)));
                    var scaled = new Bitmap(sw, sh, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(scaled))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.DrawImage(bmp, new Rectangle(0, 0, sw, sh), new Rectangle(0, 0, w, h), GraphicsUnit.Pixel);
                    }
                    toSave = scaled;
                }

                toSave.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
            }
            finally
            {
                // si hicimos un reescalado, el bmp original lo libera el using,
                // y el escalado lo liberamos aquí
                if (!ReferenceEquals(toSave, bmp))
                    toSave.Dispose();

                DeleteObject(hBmp);
            }

            return fullPath;
        }
    }
}
