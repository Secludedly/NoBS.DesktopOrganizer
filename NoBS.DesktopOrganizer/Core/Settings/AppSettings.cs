using System;

namespace NoBS.DesktopOrganizer.Core.Settings
{
    [Serializable]
    public class AppSettings
    {
        // Startup Settings
        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimizedToTaskbar { get; set; } = false;
        public bool StartMinimizedToTray { get; set; } = false;
        public string? StartupProfileName { get; set; } = null;

        // Feature Toggles
        public bool DisableSystemVolumeModifications { get; set; } = false;
        public bool DisableWallpaperModifications { get; set; } = false;
        public bool DisableVirtualDesktopSupport { get; set; } = false;
        public bool DisableLiveWindowTracking { get; set; } = false;

        // After Apply Behavior
        public MinimizeAfterApplyBehavior MinimizeAfterApply { get; set; } = MinimizeAfterApplyBehavior.AskEveryTime;

        // First run flag
        public bool HasShownMinimizePreferenceDialog { get; set; } = false;
    }

    public enum MinimizeAfterApplyBehavior
    {
        AskEveryTime = 0,
        MinimizeToTray = 1,
        MinimizeToTaskbar = 2,
        DoNotMinimize = 3
    }
}
