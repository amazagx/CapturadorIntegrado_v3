using System;
using System.Drawing;

namespace CapturadorIntegrado_v3.Core
{
    public enum AppKind { Unknown, Browser, DesktopApp }

    public sealed class TargetProfile
    {
        public string ProcessName { get; init; } = "";
        public string AppName { get; init; } = "";
        public AppKind Kind { get; init; } = AppKind.Unknown;
        public string WindowTitle { get; init; } = "";
        public Size Resolution { get; init; } = Size.Empty;
        public int ScreenIndex { get; init; } = -1;
    }

    public sealed class SessionState
    {
        public nint TargetHwnd { get; private set; } = 0;
        public string TargetTitle { get; private set; } = string.Empty;
        public DateTime StartUtc { get; private set; }
        public TimeSpan PausedAccum { get; private set; } = TimeSpan.Zero;
        public DateTime? PauseStartUtc { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsPaused { get; private set; }
        public int CaptureCount { get; private set; }
        public double ScaleFactor { get; private set; } = 1.0;
        public string DestDir { get; private set; } = "";
        public string Prefix { get; private set; } = "capturas";
        public int? TargetScreenIndex { get; private set; } = null;
        public System.Windows.Rect TargetScreenBounds { get; private set; }
        public TargetProfile? TargetProfile { get; private set; }

        public event Action? StateChanged;
        public event Action<int>? CounterChanged;

        public void Reset()
        {
            TargetHwnd = 0;
            TargetTitle = string.Empty;
            StartUtc = default;
            PausedAccum = TimeSpan.Zero;
            PauseStartUtc = null;
            IsActive = false;
            IsPaused = false;
            CaptureCount = 0;
            ScaleFactor = 1.0;
            DestDir = "";
            Prefix = "capturas";
            TargetScreenIndex = null;
            TargetScreenBounds = new System.Windows.Rect();
            TargetProfile = null;
            StateChanged?.Invoke();
        }

        public void MarkStarted(nint hwnd, string title, string destDir, string prefix, double scale, int? screenIndex, System.Windows.Rect screenBounds, TargetProfile profile, DateTime startUtc)
        {
            TargetHwnd = hwnd;
            TargetTitle = title;
            DestDir = destDir;
            Prefix = prefix;
            ScaleFactor = scale;
            TargetScreenIndex = screenIndex;
            TargetScreenBounds = screenBounds;
            TargetProfile = profile;
            StartUtc = startUtc;
            PausedAccum = TimeSpan.Zero;
            PauseStartUtc = null;
            IsActive = true;
            IsPaused = false;
            CaptureCount = 0;
            StateChanged?.Invoke();
        }

        public void TogglePause(DateTime nowUtc)
        {
            if (!IsActive) return;
            if (!IsPaused)
            {
                PauseStartUtc = nowUtc;
                IsPaused = true;
            }
            else
            {
                if (PauseStartUtc.HasValue)
                    PausedAccum += (nowUtc - PauseStartUtc.Value);
                PauseStartUtc = null;
                IsPaused = false;
            }
            StateChanged?.Invoke();
        }

        public void IncrementCounter()
        {
            CaptureCount++;
            CounterChanged?.Invoke(CaptureCount);
        }

        public void MarkEnded()
        {
            IsActive = false;
            IsPaused = false;
            StateChanged?.Invoke();
        }

        public TimeSpan ElapsedRelUtcNow(IClock clock)
        {
            if (!IsActive) return TimeSpan.Zero;
            var now = clock.UtcNow;
            var rel = now - StartUtc - PausedAccum;
            if (IsPaused && PauseStartUtc.HasValue)
            {
                rel -= (now - PauseStartUtc.Value);
            }
            if (rel < TimeSpan.Zero) rel = TimeSpan.Zero;
            return rel;
        }
    }
}