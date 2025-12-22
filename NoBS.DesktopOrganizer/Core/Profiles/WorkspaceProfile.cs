using System;
using System.Collections.Generic;

namespace NoBS.Core.Profiles
{
    public class MonitorSettings
    {
        public string DeviceName { get; set; } = ""; // e.g. \\.\DISPLAY1
        public bool Enabled { get; set; } = true;   // Whether this monitor is enabled in the profile
        public bool IsPrimary { get; set; } = false; // Which monitor should be primary
        public string AssignedMonitorDeviceName { get; set; } = ""; // e.g., "\\.\DISPLAY2"
    }

    [Serializable]
    public class WorkspaceProfile
    {
        public override string ToString()
        {
            return Name;
        }
        public string Name { get; set; } = string.Empty;
        public string AccentColor { get; set; } = "#6B5BFF";
        public List<MonitorSettings> Monitors { get; set; } = new();
        public int? SystemVolumePercent { get; set; } = 100;
        public List<WindowPosition> Windows { get; set; } = new();
        public List<WindowPosition> Apps { get; set; } = new();
        public string? WallpaperPath { get; set; } = null;

        // ============================
        // DIRTY STATE
        // ============================

        [NonSerialized]
        private bool isDirty;

        public bool IsDirty
        {
            get => isDirty;
            private set
            {
                if (isDirty == value)
                    return;

                isDirty = value;
                DirtyStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [field: NonSerialized]
        public event EventHandler? DirtyStateChanged;

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void ClearDirty()
        {
            IsDirty = false;
        }
    }

    // ============================
    // FUTURE PROFILE TYPES
    // ============================

    public class AppProfile
    {
        public List<WindowPosition> Applications { get; set; } = new();
    }

    public class WindowProfile { }
    public class MonitorProfile { }
    public class VolumeProfile { }
    public class WallpaperProfile { }
    public class CursorProfile { }
}
