using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NoBS.Core.Profiles;
using NoBS.DesktopOrganizer.Core.Helpers;
using NoBS.DesktopOrganizer.Helpers;

namespace NoBS.DesktopOrganizer.Core
{
    public sealed class ProfileApplier
    {
        private readonly AppRunner appRunner;

        public ProfileApplier(AppRunner runner)
        {
            appRunner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public async Task ApplyProfileAsync(WorkspaceProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            Helpers.Logger.LogInfo($"Applying profile: {profile.Name}");

            // -----------------------------
            // Apply system volume
            // -----------------------------
            if (profile.SystemVolumePercent.HasValue)
            {
                Helpers.Logger.LogInfo($"Setting system volume to {profile.SystemVolumePercent.Value}%");
                AudioHelper.SetSystemVolume(profile.SystemVolumePercent.Value);
            }
            else
            {
                Helpers.Logger.LogInfo("Profile has no system volume override, leaving volume unchanged");
            }

            // -----------------------------
            // Minimize all currently visible windows (only if NOT snapshot-based)
            // -----------------------------
            if (!profile.CreatedFromSnapshot)
            {
                Helpers.Logger.LogInfo("Minimizing all user windows (non-snapshot profile)");
                WindowHelper.MinimizeAllUserWindows();
            }
            else
            {
                Helpers.Logger.LogInfo("Skipping window minimization (snapshot-based profile)");
            }

            // -----------------------------
            // Kill apps marked KillOnSwitch
            // -----------------------------
            var appsToKill = profile.Apps.Where(a => a.KillOnSwitch && a.ProcessId.HasValue).ToList();
            if (appsToKill.Any())
            {
                Helpers.Logger.LogInfo($"Killing {appsToKill.Count} apps marked for termination");
                appRunner.KillAppsByProfile(profile.Apps);
            }

            // -----------------------------
            // Determine primary monitor
            // -----------------------------
            Screen primaryMonitor = Screen.PrimaryScreen;
            var allActiveScreens = Screen.AllScreens;
            Helpers.Logger.LogInfo($"Primary monitor: {primaryMonitor.DeviceName}");

            // -----------------------------
            // PRE-LAUNCH: Handle apps on disabled monitors
            // -----------------------------
            Helpers.Logger.LogInfo("Pre-launch check: Handling apps on disabled monitors");
            var appsNeedingFreshLaunch = new List<WindowPosition>();

            foreach (var app in profile.Apps)
            {
                if (app.Width <= 0 || app.Height <= 0)
                    continue;

                // Check if the saved position (app.X, app.Y) is within any active monitor's bounds
                bool isOnActiveMonitor = allActiveScreens.Any(screen =>
                    screen.Bounds.Contains(app.X, app.Y));

                if (!isOnActiveMonitor)
                {
                    Helpers.Logger.LogInfo($"App '{app.Name}' has coordinates ({app.X}, {app.Y}) on a disabled/missing monitor. Will launch fresh.");

                    // Kill the app if it's already running
                    if (app.ProcessId.HasValue)
                    {
                        try
                        {
                            var process = System.Diagnostics.Process.GetProcessById(app.ProcessId.Value);
                            if (!process.HasExited)
                            {
                                process.Kill();
                                process.WaitForExit(2000);
                                Helpers.Logger.LogInfo($"Killed existing process for '{app.Name}'");
                            }
                        }
                        catch { }
                    }

                    // Store original coordinates for reference
                    int originalX = app.X;
                    int originalY = app.Y;
                    int originalWidth = app.Width;
                    int originalHeight = app.Height;

                    // Clear coordinates to force fresh launch
                    app.X = 0;
                    app.Y = 0;
                    app.Width = 0;
                    app.Height = 0;

                    appsNeedingFreshLaunch.Add(app);
                    Helpers.Logger.LogInfo($"Cleared coordinates for '{app.Name}' - will launch fresh and reposition");
                }
            }

            // -----------------------------
            // Launch profile apps
            // -----------------------------
            Helpers.Logger.LogInfo($"Launching {profile.Apps.Count} profile apps");
            await appRunner.LaunchAppsAsync(profile.Apps);

            // -----------------------------
            // Apply wallpaper
            // -----------------------------
            if (!string.IsNullOrWhiteSpace(profile.WallpaperPath))
            {
                Helpers.Logger.LogInfo($"Attempting to apply wallpaper: {profile.WallpaperPath}");
                bool success = WallpaperHelper.ApplyWallpaperIfSafe(profile.WallpaperPath, out string errorMessage);
                if (!success)
                    Helpers.Logger.LogWarning($"Wallpaper not applied: {errorMessage}");
            }

            // -----------------------------
            // Wait for windows to appear
            // -----------------------------
            await Task.Delay(2000);

            // -----------------------------
            // Reposition apps that were launched fresh from disabled monitors
            // -----------------------------
            if (appsNeedingFreshLaunch.Any())
            {
                Helpers.Logger.LogInfo($"Repositioning {appsNeedingFreshLaunch.Count} apps launched fresh");
                await Task.Delay(1000); // Extra wait for window stabilization

                foreach (var app in appsNeedingFreshLaunch)
                {
                    if (!app.ProcessId.HasValue)
                        continue;

                    IntPtr hWnd = app.WindowHandle;
                    if (hWnd == IntPtr.Zero)
                    {
                        hWnd = WindowPositionHelper.FindWindowByProcessId(app.ProcessId.Value);
                        if (hWnd != IntPtr.Zero)
                            app.WindowHandle = hWnd;
                    }

                    if (hWnd == IntPtr.Zero)
                    {
                        Helpers.Logger.LogWarning($"Could not find window handle for '{app.Name}'");
                        continue;
                    }

                    // Get the window's ACTUAL current size (respects current monitor DPI)
                    var currentRect = WindowPositionHelper.GetWindowRectSafe(hWnd);
                    if (!currentRect.HasValue)
                    {
                        Helpers.Logger.LogWarning($"Could not get window rect for '{app.Name}'");
                        continue;
                    }

                    int actualWidth = currentRect.Value.Width;
                    int actualHeight = currentRect.Value.Height;

                    // Calculate centered position on primary monitor
                    int centerX, centerY;
                    if (actualWidth <= primaryMonitor.Bounds.Width && actualHeight <= primaryMonitor.Bounds.Height)
                    {
                        centerX = primaryMonitor.Bounds.X + (primaryMonitor.Bounds.Width - actualWidth) / 2;
                        centerY = primaryMonitor.Bounds.Y + (primaryMonitor.Bounds.Height - actualHeight) / 2;
                    }
                    else
                    {
                        centerX = primaryMonitor.Bounds.X + 50;
                        centerY = primaryMonitor.Bounds.Y + 50;
                    }

                    // Move to center of primary monitor, preserving actual size
                    WindowPositionHelper.SetWindowPosition(hWnd, centerX, centerY, actualWidth, actualHeight);

                    // Save the new position and actual size
                    app.X = centerX;
                    app.Y = centerY;
                    app.Width = actualWidth;
                    app.Height = actualHeight;
                    app.AssignedMonitorDeviceName = primaryMonitor.DeviceName;

                    Helpers.Logger.LogInfo($"Repositioned '{app.Name}' to ({centerX}, {centerY}) with size {actualWidth}x{actualHeight}");
                }
            }

            // -----------------------------
            // Force static window positions (PROFILE APPS ONLY)
            // -----------------------------
            var positioningTasks = new List<Task>();
            int appsToPosition = profile.Apps.Count(a => a.Width > 0 && a.Height > 0 && a.ProcessId.HasValue);

            if (appsToPosition > 0)
                Helpers.Logger.LogInfo($"Positioning {appsToPosition} windows");

            foreach (var app in profile.Apps)
            {
                if (app.Width <= 0 || app.Height <= 0)
                {
                    if (app.ProcessId.HasValue)
                        appRunner.StartMonitoring(app);
                    continue;
                }

                if (!app.ProcessId.HasValue)
                    continue;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        Helpers.Logger.LogInfo($"Forcing position for {app.Name} to ({app.X}, {app.Y}, {app.Width}x{app.Height})");
                        await WindowPositionHelper.ForceWindowPositionUntilStable(app);
                        Helpers.Logger.LogInfo($"Successfully positioned {app.Name}");
                        appRunner.StartMonitoring(app);
                    }
                    catch (Exception ex)
                    {
                        Helpers.Logger.LogError($"Failed to position {app.Name}", ex);
                    }
                });

                positioningTasks.Add(task);
            }

            if (positioningTasks.Any())
                await Task.WhenAll(positioningTasks);

            Helpers.Logger.LogInfo($"Profile '{profile.Name}' applied successfully");
        }
    }
}
