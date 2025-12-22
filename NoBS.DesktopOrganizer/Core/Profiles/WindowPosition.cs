using System;
using System.Drawing;
using System.Text.Json.Serialization;

namespace NoBS.Core.Profiles
{
    public class WindowPosition
    {
        // -----------------------------
        // Executable and Name
        // -----------------------------
        public string ExecutablePath { get; set; } = "";
        public string Name { get; set; } = "";

        // -----------------------------
        // Saved window position & size
        // -----------------------------
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // -----------------------------
        // Runtime info
        // -----------------------------
        [JsonIgnore]
        public int? ProcessId { get; set; } = null;
        [JsonIgnore]
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;
        [JsonIgnore]
        public AppRunStatus Status { get; set; } = AppRunStatus.NotRunning;
        [JsonIgnore]
        public string? LastError { get; set; }

        // -----------------------------
        // Mission 2 / monitoring
        // -----------------------------
        [JsonIgnore]
        public Rectangle LastBounds { get; set; } = Rectangle.Empty;
        [JsonIgnore]
        public long LastStyle { get; set; } = 0;

        // Force position for a duration
        [JsonIgnore]
        public bool ForcePositionActive { get; set; } = false;
        [JsonIgnore]
        public DateTime ForceUntil { get; set; } = DateTime.MinValue;

        // Wait for stable window before applying position
        [JsonIgnore]
        public bool WaitForStableWindow { get; set; } = false;

        // Optional flags
        public bool KillOnSwitch { get; set; } = false;
        public int LaunchDelaySeconds { get; set; } = 0;
        public string AssignedMonitorDeviceName { get; set; } = "";

        // Constructor
        public WindowPosition() { }

        public WindowPosition(string exePath, string name, int x, int y, int width, int height)
        {
            ExecutablePath = exePath;
            Name = name;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public enum AppRunStatus
    {
        NotRunning,
        Launching,
        Running,
        Failed
    }
}
