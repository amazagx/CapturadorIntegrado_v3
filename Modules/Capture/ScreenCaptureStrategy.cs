using System.Drawing;
using CapturadorIntegrado_v3.Core;

namespace CapturadorIntegrado_v3.Modules.Capture
{
    public sealed class ScreenCaptureStrategy : ICaptureStrategy
    {
        public string Name => "Screen";

        public Bitmap? Capture(SessionState s)
        {
            var r = s.TargetScreenBounds;
            int width = (int)r.Width;
            int height = (int)r.Height;
            if (width <= 0 || height <= 0) return null;

            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen((int)r.X, (int)r.Y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            return bmp;
        }
    }
}