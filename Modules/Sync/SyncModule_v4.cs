using System;
using System.IO;
using System.Text;
using CapturadorIntegrado_v3.Core;

namespace CapturadorIntegrado_v3.Modules.Sync
{
    public sealed class SyncModule_v4
    {
        private readonly IClock _clock;
        private readonly ILogger _log;

        public SyncModule_v4(IClock clock, ILogger log)
        {
            _clock = clock;
            _log = log;
        }

        public void SaveSessionSummary(SessionState s)
        {
            try
            {
                string path = Path.Combine(s.DestDir, "session.txt");
                var sb = new StringBuilder();
                sb.AppendLine($"start_abs={s.StartUtc:O}");
                sb.AppendLine($"end_abs={_clock.UtcNow:O}");
                sb.AppendLine($"dest_dir={s.DestDir}");
                sb.AppendLine($"prefix={s.Prefix}");
                sb.AppendLine($"scale={s.ScaleFactor}");
                sb.AppendLine($"captures={s.CaptureCount}");
                if (s.TargetProfile != null)
                {
                    sb.AppendLine($"target.kind={s.TargetProfile.Kind}");
                    sb.AppendLine($"target.app={s.TargetProfile.AppName}");
                    sb.AppendLine($"target.process={s.TargetProfile.ProcessName}");
                    sb.AppendLine($"target.title={s.TargetProfile.WindowTitle}");
                    sb.AppendLine($"target.resolution={s.TargetProfile.Resolution.Width}x{s.TargetProfile.Resolution.Height}");
                    sb.AppendLine($"target.screenIndex={s.TargetProfile.ScreenIndex}");
                }
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                _log.Info("sync", "summary_saved", "file='session.txt'");
            }
            catch (Exception ex)
            {
                _log.Warn("sync", "summary_fail", ex.Message);
            }
        }
    }
}