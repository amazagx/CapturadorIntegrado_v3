using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CapturadorIntegrado_v3.Core;

namespace CapturadorIntegrado_v3.Modules.Capture
{
    public sealed class CaptureModule_v4
    {
        private readonly ICaptureStrategy _windowStrategy;
        private readonly ICaptureStrategy _screenStrategy;
        private readonly FeatureFlags _flags;
        private readonly NamingPolicy _naming;
        private readonly IClock _clock;
        private readonly ILogger _log;

        public event Action<string>? CaptureSaved;

        public CaptureModule_v4(ICaptureStrategy windowStrategy, ICaptureStrategy screenStrategy,
            FeatureFlags flags, NamingPolicy naming, IClock clock, ILogger log)
        {
            _windowStrategy = windowStrategy;
            _screenStrategy = screenStrategy;
            _flags = flags;
            _naming = naming;
            _clock = clock;
            _log = log;
        }

        private static bool IsBlackFrame(Bitmap bmp, byte lumaThreshold = 8)
        {
            int steps = 16;
            int w = bmp.Width;
            int h = bmp.Height;
            if (w <= 2 || h <= 2) return true;
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* scan0 = (byte*)data.Scan0;
                    int stride = data.Stride;
                    for (int yi = 1; yi < steps; yi++)
                    {
                        int y = yi * (h - 1) / steps;
                        for (int xi = 1; xi < steps; xi++)
                        {
                            int x = xi * (w - 1) / steps;
                            byte* p = scan0 + y * stride + x * 4;
                            byte b = p[0], g = p[1], r = p[2];
                            int luma = (int)(0.2126 * r + 0.7152 * g + 0.0722 * b);
                            if (luma > lumaThreshold) return false;
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return true;
        }

        private string BuildFullPath(SessionState s, TimeSpan elapsed)
        {
            Directory.CreateDirectory(s.DestDir);
            string name = _naming.BuildFileName(s.Prefix, s.CaptureCount + 1, elapsed);
            return Path.Combine(s.DestDir, name);
        }

        public bool CaptureOnce(SessionState s, out string? fullPath, bool isProbe = false)
        {
            fullPath = null;
            if (!s.IsActive) return false;

            Bitmap? bmp = _windowStrategy.Capture(s);
            bool usedWindow = true;
            if (bmp == null)
            {
                _log.Warn("capture", "window_null", null);
            }
            else if (IsBlackFrame(bmp, _flags.BlackFrameLumaThreshold))
            {
                _log.Warn("capture", "black_window", null);
                bmp.Dispose();
                bmp = null;
            }

            if (bmp == null && _flags.FallbackToScreenOnBlack)
            {
                bmp = _screenStrategy.Capture(s);
                usedWindow = false;
                if (bmp == null) _log.Error("capture", "screen_null", null);
            }

            if (bmp == null) return false;

            try
            {
                var elapsed = s.ElapsedRelUtcNow(_clock);
                if (isProbe)
                {
                    string probeName = $"{s.Prefix}_PROBE.png";
                    fullPath = Path.Combine(s.DestDir, probeName);
                }
                else
                {
                    fullPath = BuildFullPath(s, elapsed);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                bmp.Save(fullPath, ImageFormat.Png);
                bmp.Dispose();

                if (!isProbe)
                    s.IncrementCounter();

                string extra = $"strategy={(usedWindow ? "Window" : "Screen")} file='" + Path.GetFileName(fullPath) + "'";
                _log.Info("capture", "saved", extra);
                CaptureSaved?.Invoke(fullPath!);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error("capture", "save_fail", ex.Message);
                try { bmp.Dispose(); } catch { }
                return false;
            }
        }
    }
}