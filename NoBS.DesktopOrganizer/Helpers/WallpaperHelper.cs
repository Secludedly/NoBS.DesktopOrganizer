using System;
using System.IO;
using System.Runtime.InteropServices;
using NoBS.DesktopOrganizer.Core.Helpers;

namespace NoBS.DesktopOrganizer.Helpers
{
    public static class WallpaperHelper
    {
        // ============================
        // Win32 API Declarations
        // ============================

        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SystemParametersInfo(
            int uAction,
            int uParam,
            string lpvParam,
            int fuWinIni);

        // ============================
        // Get Current Wallpaper
        // ============================

        public static string? GetCurrentWallpaper()
        {
            try
            {
                // Windows stores current wallpaper in registry
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Control Panel\Desktop", false);

                if (key == null)
                {
                    Logger.LogWarning("Cannot access Desktop registry key");
                    return null;
                }

                string? wallpaper = key.GetValue("Wallpaper") as string;

                if (string.IsNullOrEmpty(wallpaper))
                {
                    Logger.LogInfo("No wallpaper currently set (solid color background)");
                    return null;
                }

                Logger.LogInfo($"Current wallpaper: {wallpaper}");
                return wallpaper;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to get current wallpaper", ex);
                return null;
            }
        }

        // ============================
        // Set Wallpaper
        // ============================

        public static bool SetWallpaper(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                Logger.LogWarning("SetWallpaper called with empty path");
                return false;
            }

            try
            {
                // Verify file exists
                if (!File.Exists(imagePath))
                {
                    Logger.LogWarning($"Wallpaper file not found: {imagePath}");
                    return false;
                }

                // Verify file is an image (basic check by extension)
                string ext = Path.GetExtension(imagePath).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" &&
                    ext != ".bmp" && ext != ".gif")
                {
                    Logger.LogWarning($"Unsupported wallpaper format: {ext}");
                    return false;
                }

                Logger.LogInfo($"Setting wallpaper to: {imagePath}");

                // Call Win32 API to set wallpaper
                int result = SystemParametersInfo(
                    SPI_SETDESKWALLPAPER,
                    0,
                    imagePath,
                    SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                if (result == 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.LogError($"SystemParametersInfo failed with error code: {error}");
                    return false;
                }

                Logger.LogInfo("Wallpaper changed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to set wallpaper", ex);
                return false;
            }
        }

        // ============================
        // WallpaperEngine Detection
        // ============================

        public static bool IsWallpaperEngineRunning()
        {
            try
            {
                // Check for common WallpaperEngine process names
                var processes = System.Diagnostics.Process.GetProcessesByName("wallpaper32");
                if (processes.Length > 0)
                {
                    Logger.LogInfo("WallpaperEngine detected (wallpaper32.exe)");
                    return true;
                }

                processes = System.Diagnostics.Process.GetProcessesByName("wallpaper64");
                if (processes.Length > 0)
                {
                    Logger.LogInfo("WallpaperEngine detected (wallpaper64.exe)");
                    return true;
                }

                // Some versions use this name
                processes = System.Diagnostics.Process.GetProcessesByName("wallpaperengine");
                if (processes.Length > 0)
                {
                    Logger.LogInfo("WallpaperEngine detected (wallpaperengine.exe)");
                    return true;
                }

                Logger.LogInfo("WallpaperEngine not detected");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to check for WallpaperEngine: {ex.Message}");
                // If we can't check, assume it's NOT running to avoid blocking wallpaper changes
                return false;
            }
        }

        // ============================
        // Apply Wallpaper with Safety Checks
        // ============================

        public static bool ApplyWallpaperIfSafe(string? wallpaperPath, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Null or empty path means "don't change wallpaper"
            if (string.IsNullOrWhiteSpace(wallpaperPath))
            {
                Logger.LogInfo("No wallpaper path specified, skipping wallpaper change");
                return true; // Not an error, just a no-op
            }

            // Check if WallpaperEngine is running
            if (IsWallpaperEngineRunning())
            {
                errorMessage = "Wallpaper not changed: WallpaperEngine is running";
                Logger.LogInfo(errorMessage);
                return false;
            }

            // Check if file exists
            if (!File.Exists(wallpaperPath))
            {
                errorMessage = $"Wallpaper file not found: {Path.GetFileName(wallpaperPath)}";
                Logger.LogWarning($"Wallpaper file missing: {wallpaperPath}");
                return false;
            }

            // Attempt to set wallpaper
            bool success = SetWallpaper(wallpaperPath);
            if (!success)
            {
                errorMessage = "Failed to apply wallpaper (system error)";
            }

            return success;
        }

        // ============================
        // Image Validation
        // ============================

        public static bool IsValidImageFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (!File.Exists(filePath))
                return false;

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" ||
                   ext == ".bmp" || ext == ".gif";
        }
    }
}
