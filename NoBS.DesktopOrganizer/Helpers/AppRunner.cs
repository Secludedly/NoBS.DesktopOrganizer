using NoBS.Core.Profiles;
using NoBS.DesktopOrganizer.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NoBS.DesktopOrganizer.Core
{
    public class AppRunner
    {
        // Global tracking of all running apps across all profiles
        private static readonly Dictionary<string, AppTrackingInfo> globalAppRegistry = new();
        private static readonly object registryLock = new object();

        private readonly List<Process> launchedProcesses = new();
        private readonly Dictionary<int, CancellationTokenSource> monitorTokens = new();

        // Helper class to track app information globally
        private class AppTrackingInfo
        {
            public int ProcessId { get; set; }
            public IntPtr WindowHandle { get; set; }
            public AppRunStatus Status { get; set; }
            public string? LastError { get; set; }
            public Rectangle LastBounds { get; set; }
            public long LastStyle { get; set; }
        }

        // Launch a single app
        public async Task LaunchAppsAsync(WindowPosition app)
        {
            Logger.LogInfo($"Launching app: {app.Name} ({app.ExecutablePath})");

            app.Status = AppRunStatus.Launching;
            app.LastError = null;

            // Update global registry
            UpdateGlobalRegistry(app);

            try
            {
                var proc = Process.Start(app.ExecutablePath);
                if (proc == null)
                    throw new Exception("Process failed to start.");

                app.ProcessId = proc.Id;
                launchedProcesses.Add(proc);

                Logger.LogInfo($"App launched successfully: {app.Name} (PID: {proc.Id})");

                // Update global registry with process ID
                UpdateGlobalRegistry(app);

                // Don't start monitoring yet - let ProfileApplier position the window first
                // StartMonitoring will be called after positioning is complete

                // Wait for main window to appear
                for (int i = 0; i < 50; i++)
                {
                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        app.WindowHandle = proc.MainWindowHandle;
                        app.Status = AppRunStatus.Running;
                        UpdateGlobalRegistry(app);
                        Logger.LogInfo($"Window found for {app.Name} (HWND: {app.WindowHandle.ToString("X")})");
                        break;
                    }
                    await Task.Delay(100);
                }

                // If window never appeared
                if (app.WindowHandle == IntPtr.Zero)
                {
                    app.Status = AppRunStatus.Failed;
                    app.LastError = "Window never appeared";
                    UpdateGlobalRegistry(app);
                    Logger.LogWarning($"Window never appeared for {app.Name}");
                }
            }
            catch (Exception ex)
            {
                app.Status = AppRunStatus.Failed;
                app.LastError = ex.Message;
                UpdateGlobalRegistry(app);
                Logger.LogError($"Failed to launch {app.Name}", ex);
            }
        }

        // Launch multiple apps
        public async Task LaunchAppsAsync(IEnumerable<WindowPosition> apps)
        {
            if (apps == null) return;

            foreach (var app in apps)
            {
                // Respect launch delay
                if (app.LaunchDelaySeconds > 0)
                    await Task.Delay(app.LaunchDelaySeconds * 1000);

                await LaunchAppsAsync(app);
            }
        }

        // Kill all apps launched by this runner
        public void KillApps()
        {
            foreach (var proc in launchedProcesses.ToList())
            {
                try
                {
                    if (proc.HasExited) continue;
                    if (proc.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase)) continue;
                    proc.Kill(true);
                }
                catch { }
            }
            launchedProcesses.Clear();
        }

        // Kill apps associated with a profile
        public void KillAppsByProfile(List<WindowPosition> apps)
        {
            foreach (var app in apps)
            {
                if (!app.ProcessId.HasValue) continue;

                try
                {
                    var proc = Process.GetProcessById(app.ProcessId.Value);
                    if (!proc.HasExited) proc.Kill(true);
                }
                catch { }

                StopMonitoring(app);

                app.ProcessId = null;
                app.WindowHandle = IntPtr.Zero;
                app.Status = AppRunStatus.NotRunning;

                // Update global registry to reflect app is no longer running
                UpdateGlobalRegistry(app);
            }
        }

        // -----------------------------
        // MONITORING LOGIC
        // -----------------------------
        public void StartMonitoring(WindowPosition app)
        {
            StopMonitoring(app); // Cancel old monitor if exists

            if (!app.ProcessId.HasValue) return;

            // Initialize LastBounds with current saved position to prevent immediate overwrite
            if (app.Width > 0 && app.Height > 0 && app.LastBounds == Rectangle.Empty)
            {
                app.LastBounds = new Rectangle(app.X, app.Y, app.Width, app.Height);
            }

            var cts = new CancellationTokenSource();
            monitorTokens[app.ProcessId.Value] = cts;

            Task.Run(async () =>
            {
                var token = cts.Token;

                while (!token.IsCancellationRequested)
                {
                    if (!app.ProcessId.HasValue) break;

                    try
                    {
                        var proc = Process.GetProcessById(app.ProcessId.Value);
                        if (proc.HasExited)
                        {
                            app.Status = AppRunStatus.NotRunning;
                            app.ProcessId = null;
                            app.WindowHandle = IntPtr.Zero;
                            UpdateGlobalRegistry(app); // Remove from global registry
                            break;
                        }

                        IntPtr hWnd = proc.MainWindowHandle;
                        if (hWnd == IntPtr.Zero)
                        {
                            app.WindowHandle = IntPtr.Zero;
                            await Task.Delay(200);
                            continue;
                        }

                        app.WindowHandle = hWnd;

                        // Get current window rectangle and style
                        var rect = WindowPositionHelper.GetWindowRectSafe(hWnd);
                        var style = WindowPositionHelper.GetWindowStyle(hWnd);

                        // Update profile coordinates if changed
                        if (rect.HasValue && (rect.Value != app.LastBounds || style != app.LastStyle))
                        {
                            app.LastBounds = rect.Value;
                            app.LastStyle = style;

                            app.X = rect.Value.X;
                            app.Y = rect.Value.Y;
                            app.Width = rect.Value.Width;
                            app.Height = rect.Value.Height;

                            // Update global registry with new position
                            UpdateGlobalRegistry(app);
                        }

                        // Force position if active
                        if (app.ForcePositionActive && DateTime.Now < app.ForceUntil)
                        {
                            WindowPositionHelper.SetWindowPosition(hWnd, app.X, app.Y, app.Width, app.Height);
                        }

                        // Optional: wait for stable window
                        if (app.WaitForStableWindow)
                            await Task.Delay(300);
                    }
                    catch
                    {
                        app.Status = AppRunStatus.NotRunning;
                        app.ProcessId = null;
                        app.WindowHandle = IntPtr.Zero;
                        UpdateGlobalRegistry(app); // Remove from global registry
                        break;
                    }

                    await Task.Delay(200); // 5 updates/sec
                }
            });
        }

        private void StopMonitoring(WindowPosition app)
        {
            if (!app.ProcessId.HasValue) return;
            if (monitorTokens.TryGetValue(app.ProcessId.Value, out var cts))
            {
                cts.Cancel();
                monitorTokens.Remove(app.ProcessId.Value);
            }
        }

        // Refresh the status of a list of apps (used in UI updates)
        public void RefreshAppStatuses(IEnumerable<WindowPosition> apps)
        {
            foreach (var app in apps)
            {
                // First, check the global registry for this app
                string key = GetRegistryKey(app.ExecutablePath);
                lock (registryLock)
                {
                    if (globalAppRegistry.TryGetValue(key, out var info))
                    {
                        // Verify the process is still running
                        try
                        {
                            var proc = Process.GetProcessById(info.ProcessId);
                            if (!proc.HasExited)
                            {
                                // Restore information from global registry
                                app.ProcessId = info.ProcessId;
                                app.WindowHandle = info.WindowHandle;
                                app.Status = info.Status;
                                app.LastError = info.LastError;
                                app.LastBounds = info.LastBounds;
                                app.LastStyle = info.LastStyle;
                                continue;
                            }
                            else
                            {
                                // Process has exited, remove from registry
                                globalAppRegistry.Remove(key);
                            }
                        }
                        catch
                        {
                            // Process not found, remove from registry
                            globalAppRegistry.Remove(key);
                        }
                    }
                }

                // If not in global registry, check if we have a ProcessId
                if (!app.ProcessId.HasValue)
                {
                    app.Status = AppRunStatus.NotRunning;
                    app.WindowHandle = IntPtr.Zero;
                    continue;
                }

                // Check if the process is still running
                try
                {
                    var proc = Process.GetProcessById(app.ProcessId.Value);
                    if (proc.HasExited)
                    {
                        app.Status = AppRunStatus.NotRunning;
                        app.ProcessId = null;
                        app.WindowHandle = IntPtr.Zero;
                    }
                    else
                    {
                        app.Status = AppRunStatus.Running;
                        app.WindowHandle = proc.MainWindowHandle;
                        UpdateGlobalRegistry(app);
                    }
                }
                catch
                {
                    app.Status = AppRunStatus.NotRunning;
                    app.ProcessId = null;
                    app.WindowHandle = IntPtr.Zero;
                }
            }
        }

        // -----------------------------
        // GLOBAL REGISTRY HELPERS
        // -----------------------------
        private static string GetRegistryKey(string executablePath)
        {
            // Normalize the path to ensure consistent keys
            try
            {
                return System.IO.Path.GetFullPath(executablePath).ToLowerInvariant();
            }
            catch
            {
                return executablePath.ToLowerInvariant();
            }
        }

        private static void UpdateGlobalRegistry(WindowPosition app)
        {
            if (string.IsNullOrEmpty(app.ExecutablePath))
                return;

            string key = GetRegistryKey(app.ExecutablePath);

            lock (registryLock)
            {
                if (app.Status == AppRunStatus.NotRunning || !app.ProcessId.HasValue)
                {
                    // Remove from registry if not running
                    globalAppRegistry.Remove(key);
                }
                else
                {
                    // Update or add to registry
                    globalAppRegistry[key] = new AppTrackingInfo
                    {
                        ProcessId = app.ProcessId.Value,
                        WindowHandle = app.WindowHandle,
                        Status = app.Status,
                        LastError = app.LastError,
                        LastBounds = app.LastBounds,
                        LastStyle = app.LastStyle
                    };
                }
            }
        }
    }
}
