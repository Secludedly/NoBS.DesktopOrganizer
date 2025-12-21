using System;
using System.Collections.Generic;

namespace NoBS.Core.Profiles
{
    [Serializable]
    public class WorkspaceProfile
    {
        public string Name { get; set; } = string.Empty;
        public string AccentColor { get; set; } = "#6B5BFF";

        public List<WindowPosition> Windows { get; set; } = new();
        public List<WindowPosition> Apps { get; set; } = new();

        // Wallpaper configuration (optional)
        public string? WallpaperPath { get; set; } = null;

        // ============================
        // DIRTY STATE (Mission 1)
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
    // FUTURE PROFILE TYPES (KEEP)
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
