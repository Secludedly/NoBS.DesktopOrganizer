using System;
using System.Collections.Generic;
using System.Linq;
using WindowsDesktop;

namespace NoBS.DesktopOrganizer.Core.Helpers
{
    public static class VirtualDesktopHelper
    {
        /// <summary>
        /// Checks if Virtual Desktop API is available on this Windows version
        /// </summary>
        public static bool IsSupported()
        {
            try
            {
                VirtualDesktop.GetDesktops();
                return true;
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException comEx)
                    Logger.LogWarning($"Virtual Desktop API COM error: HRESULT 0x{comEx.HResult:X}");
                else
                    Logger.LogWarning($"Virtual Desktop API not available: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all virtual desktops with fallback names
        /// </summary>
        public static List<DesktopInfo> GetAllDesktops()
        {
            try
            {
                var desktops = VirtualDesktop.GetDesktops();
                var result = new List<DesktopInfo>();

                for (int i = 0; i < desktops.Length; i++)
                {
                    var desktop = desktops[i];
                    string displayName = string.IsNullOrWhiteSpace(desktop.Name)
                        ? $"Desktop {i + 1}"
                        : desktop.Name;

                    result.Add(new DesktopInfo
                    {
                        Id = desktop.Id.ToString(),
                        Name = displayName,
                        IsCurrentDesktop = VirtualDesktop.Current?.Id == desktop.Id
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to get virtual desktops", ex);
                return new List<DesktopInfo>();
            }
        }

        /// <summary>
        /// Finds desktop by GUID string
        /// </summary>
        public static VirtualDesktop? FindDesktopById(string? desktopId)
        {
            if (string.IsNullOrWhiteSpace(desktopId))
                return null;

            try
            {
                var guid = Guid.Parse(desktopId);
                var desktop = VirtualDesktop.FromId(guid);
                return desktop;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Switches to specified desktop
        /// </summary>
        public static bool SwitchToDesktop(string? desktopId)
        {
            var desktop = FindDesktopById(desktopId);
            if (desktop == null)
                return false;

            try
            {
                desktop.Switch();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to switch to desktop {desktopId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Renames desktop to specified name
        /// </summary>
        public static bool RenameDesktop(string? desktopId, string newName)
        {
            var desktop = FindDesktopById(desktopId);
            if (desktop == null || string.IsNullOrWhiteSpace(newName))
                return false;

            try
            {
                // Attempt to set the Name property
                // Note: This may not be supported in all versions of the VirtualDesktop library
                desktop.Name = newName;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to rename desktop {desktopId} to '{newName}'", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets current desktop's GUID
        /// </summary>
        public static string? GetCurrentDesktopId()
        {
            try
            {
                var current = VirtualDesktop.Current;
                return current?.Id.ToString();
            }
            catch
            {
                return null;
            }
        }
    }

    public class DesktopInfo
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsCurrentDesktop { get; set; }

        public override string ToString() => Name;
    }
}
