using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using CapturadorIntegrado_v3.Modules.Playback;
using CapturadorIntegrado_v3.Modules.Capture;
using CapturadorIntegrado_v3.Modules.Sync;

namespace CapturadorIntegrado_v3.Core
{
    public interface ISessionCommands
    {
        bool ProbeTarget(string targetTitle, int? screenIndex, string destDir, string prefix, double scale, out string? samplePath, out string? message);
        bool Start(string targetTitle, string destDir, string prefix, double scale, int? screenIndex);
        bool TogglePause();
        bool CaptureOnce();
        bool End();
    }

    public sealed class SessionCommands : ISessionCommands
    {
        private readonly SessionState _s;
        private readonly StarterModule_v4 _start;
        private readonly PauseModule_v4 _pause;
        private readonly CaptureModule_v4 _capture;
        private readonly SyncModule_v4 _sync;
        private readonly IClock _clock;
        private readonly ILogger _log;
        private readonly FeatureFlags _flags;

        public SessionCommands(SessionState s, StarterModule_v4 start, PauseModule_v4 pause, CaptureModule_v4 capture, SyncModule_v4 sync, IClock clock, ILogger log, FeatureFlags flags)
        {
            _s = s; _start = start; _pause = pause; _capture = capture; _sync = sync; _clock = clock; _log = log; _flags = flags;
        }

        private static int ResolveScreenIndex(int? requestedIndex, nint hwnd)
        {
            if (requestedIndex.HasValue && requestedIndex.Value >= 0 && requestedIndex.Value < Screen.AllScreens.Length)
                return requestedIndex.Value;
            IntPtr mon = Win32.MonitorFromWindow((IntPtr)hwnd, Win32.MONITOR_DEFAULTTONEAREST);
            var screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var sc = screens[i];
                if (sc.Primary) return i;
            }
            return 0;
        }

        private static string MapAppName(string proc) => proc.ToLowerInvariant() switch
        {
            "chrome" => "Chrome",
            "msedge" => "Edge",
            "firefox" => "Firefox",
            "vlc" => "VLC",
            _ => proc
        };

        private static AppKind MapKind(string proc) => proc.ToLowerInvariant() switch
        {
            "chrome" or "msedge" or "firefox" => AppKind.Browser,
            _ => AppKind.DesktopApp
        };

        private static System.Windows.Rect ToRect(System.Drawing.Rectangle r) => new System.Windows.Rect(r.Left, r.Top, r.Width, r.Height);

        private bool ResolveAndStampSession(string targetTitle, string destDir, string prefix, double scale, int? screenIndex, bool dryRun, out string? msg)
        {
            msg = null;
            var hwnd = Win32.FindFirstWindowByTitleSubstring(targetTitle);
            if (hwnd == IntPtr.Zero) { msg = "No se encontró ninguna ventana con ese título."; return false; }

            string windowTitle = Win32.GetWindowTitle(hwnd);
            Win32.GetWindowRect(hwnd, out var r);
            var size = new System.Drawing.Size(Math.Max(1, r.Right - r.Left), Math.Max(1, r.Bottom - r.Top));

            Win32.GetWindowThreadProcessId(hwnd, out uint pid);
            string procName = "";
            try { procName = Process.GetProcessById((int)pid).ProcessName; } catch { procName = "unknown"; }

            int scrIndex = ResolveScreenIndex(screenIndex, hwnd);
            var scr = Screen.AllScreens[Math.Max(0, Math.Min(Screen.AllScreens.Length - 1, scrIndex))];
            var bounds = ToRect(scr.Bounds);

            var profile = new TargetProfile
            {
                ProcessName = procName,
                AppName = MapAppName(procName),
                Kind = MapKind(procName),
                WindowTitle = windowTitle,
                Resolution = size,
                ScreenIndex = scrIndex
            };

            if (!dryRun)
            {
                _s.MarkStarted((nint)hwnd, windowTitle, destDir, prefix, scale, screenIndex, bounds, profile, _clock.UtcNow);
                _log.Info("timeline", "start", $"target='{windowTitle}' screen={scrIndex}");
            }
            else
            {
                // probe: no fija estado
            }
            return true;
        }

        public bool ProbeTarget(string targetTitle, int? screenIndex, string destDir, string prefix, double scale, out string? samplePath, out string? message)
        {
            samplePath = null; message = null;
            if (!ResolveAndStampSession(targetTitle, destDir, prefix, scale, screenIndex, dryRun: false, out message))
                return false;

            if (!_capture.CaptureOnce(_s, out samplePath, isProbe: true))
            {
                message = "La captura de prueba ha fallado.";
                return false;
            }
            _s.MarkEnded();
            return true;
        }

        public bool Start(string targetTitle, string destDir, string prefix, double scale, int? screenIndex)
        {
            if (!ResolveAndStampSession(targetTitle, destDir, prefix, scale, screenIndex, dryRun: false, out string? msg))
            {
                _log.Warn("start", "resolve_fail", msg);
                return false;
            }
            _start.StartPlayback(_s.TargetHwnd, _log);
            return true;
        }

        public bool TogglePause()
        {
            if (!_s.IsActive) return false;
            _s.TogglePause(_clock.UtcNow);
            _pause.Toggle(_s.TargetHwnd, _log);
            return true;
        }

        public bool CaptureOnce()
        {
            if (!_s.IsActive) return false;
            return _capture.CaptureOnce(_s, out _);
        }

        public bool End()
        {
            if (!_s.IsActive) { _s.Reset(); return true; }
            _sync.SaveSessionSummary(_s);
            _s.MarkEnded();
            _s.Reset();
            return true;
        }
    }
}