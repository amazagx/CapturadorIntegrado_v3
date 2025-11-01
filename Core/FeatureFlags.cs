namespace CapturadorIntegrado_v3.Core
{
    public sealed class FeatureFlags
    {
        public bool UseUIAForPlayback { get; set; } = false;
        public bool FallbackToScreenOnBlack { get; set; } = true;
        public byte BlackFrameLumaThreshold { get; set; } = 8; // 0..255
        public bool RequireTargetVerification { get; set; } = true;
        public int HotkeyModifierMask { get; set; } = (MOD_ALT | MOD_CONTROL);

        // Win32 modifiers
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;
    }
}