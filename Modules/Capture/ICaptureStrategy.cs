using System.Drawing;
using CapturadorIntegrado_v3.Core;

namespace CapturadorIntegrado_v3.Modules.Capture
{
    public interface ICaptureStrategy
    {
        Bitmap? Capture(SessionState s);
        string Name { get; }
    }
}