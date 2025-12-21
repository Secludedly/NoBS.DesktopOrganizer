using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            // Minimize all currently visible windows (NOT profile apps yet)
            // -----------------------------
            Helpers.Logger.LogInfo("Minimizing all user windows");
            WindowHelper.MinimizeAllUserWindows();

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
            // Launch profile apps
            // -----------------------------
            Helpers.Logger.LogInfo($"Launching {profile.Apps.Count} profile apps");
            await appRunner.LaunchAppsAsync(profile.Apps);

            // -----------------------------
            // Apply wallpaper (if specified and safe)
            // -----------------------------
            if (!string.IsNullOrWhiteSpace(profile.WallpaperPath))
            {
                Helpers.Logger.LogInfo($"Attempting to apply wallpaper: {profile.WallpaperPath}");

                bool success = WallpaperHelper.ApplyWallpaperIfSafe(
                    profile.WallpaperPath,
                    out string errorMessage);

                if (!success)
                {
                    Helpers.Logger.LogWarning($"Wallpaper not applied: {errorMessage}");
                    // Don't throw - wallpaper failure shouldn't stop profile application
                }
            }
            else
            {
                Helpers.Logger.LogInfo("Profile has no wallpaper configured, skipping wallpaper change");
            }

            // -----------------------------
            // Wait a little for initial windows to appear
            // -----------------------------
            await Task.Delay(2000);

            // -----------------------------
            // Force static window positions (PROFILE APPS ONLY)
            // -----------------------------
            var positioningTasks = new List<Task>();
            int appsToPosition = profile.Apps.Count(a => a.Width > 0 && a.Height > 0 && a.ProcessId.HasValue);

            if (appsToPosition > 0)
            {
                Helpers.Logger.LogInfo($"Positioning {appsToPosition} windows");
            }

            foreach (var app in profile.Apps)
            {
                // Skip if user never saved a position
                if (app.Width <= 0 || app.Height <= 0)
                {
                    Helpers.Logger.LogWarning($"Skipping {app.Name}: No saved position");
                    // Still start monitoring if the process launched
                    if (app.ProcessId.HasValue)
                    {
                        appRunner.StartMonitoring(app);
                    }
                    continue;
                }

                // Skip if process never started
                if (!app.ProcessId.HasValue)
                {
                    Helpers.Logger.LogWarning($"Skipping {app.Name}: Process did not start");
                    continue;
                }

                // Launch positioning task for each app
                var task = Task.Run(async () =>
                {
                    try
                    {
                        Helpers.Logger.LogInfo($"Forcing position for {app.Name} to ({app.X}, {app.Y}, {app.Width}x{app.Height})");
                        await WindowPositionHelper.ForceWindowPositionUntilStable(app);
                        Helpers.Logger.LogInfo($"Successfully positioned {app.Name}");

                        // Now start monitoring to track position changes
                        appRunner.StartMonitoring(app);
                    }
                    catch (Exception ex)
                    {
                        Helpers.Logger.LogError($"Failed to position {app.Name}", ex);
                    }
                });

                positioningTasks.Add(task);
            }

            // Wait for all positioning tasks to complete (with timeout)
            if (positioningTasks.Any())
            {
                await Task.WhenAll(positioningTasks);
                Helpers.Logger.LogInfo("All positioning tasks completed");
            }

            Helpers.Logger.LogInfo($"Profile '{profile.Name}' applied successfully");
        }
    }
}

