using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace NoBS.DesktopOrganizer.Core.Helpers
{
    public static class WindowPositionHelper
    {
        // -----------------------------
        // Win32 API Imports
        // -----------------------------
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int GWL_STYLE = -16;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        // -----------------------------
        // Get window rectangle safely
        // -----------------------------
        public static Rectangle? GetWindowRectSafe(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return null;

            try
            {
                if (GetWindowRect(hWnd, out RECT rect))
                {
                    return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                }
            }
            catch { }

            return null;
        }

        // -----------------------------
        // Get window style
        // -----------------------------
        public static long GetWindowStyle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return 0;

            try
            {
                return GetWindowLong(hWnd, GWL_STYLE);
            }
            catch
            {
                return 0;
            }
        }

        // -----------------------------
        // Set window position and size
        // -----------------------------
        public static void SetWindowPosition(IntPtr hWnd, int x, int y, int width, int height)
        {
            if (hWnd == IntPtr.Zero) return;

            try
            {
                MoveWindow(hWnd, x, y, width, height, true);
            }
            catch { }
        }

        // -----------------------------
        // Find window by executable path
        // -----------------------------
        public static IntPtr FindWindowByExecutable(string executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
                return IntPtr.Zero;

            IntPtr foundWindow = IntPtr.Zero;
            string normalizedPath = System.IO.Path.GetFullPath(executablePath).ToLowerInvariant();

            EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    if (!IsWindowVisible(hWnd))
                        return true;

                    GetWindowThreadProcessId(hWnd, out uint processId);
                    if (processId == 0)
                        return true;

                    var process = System.Diagnostics.Process.GetProcessById((int)processId);
                    if (process.HasExited)
                        return true;

                    string processPath = process.MainModule?.FileName?.ToLowerInvariant() ?? "";
                    if (processPath == normalizedPath)
                    {
                        foundWindow = hWnd;
                        return false; // Stop enumeration
                    }
                }
                catch
                {
                    // Ignore access denied or other exceptions
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            return foundWindow;
        }

        // -----------------------------
        // Find window by process ID
        // -----------------------------
        public static IntPtr FindWindowByProcessId(int processId)
        {
            if (processId <= 0)
                return IntPtr.Zero;

            IntPtr foundWindow = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    if (!IsWindowVisible(hWnd))
                        return true;

                    GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == processId)
                    {
                        foundWindow = hWnd;
                        return false; // Stop enumeration
                    }
                }
                catch
                {
                    // Ignore exceptions
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            return foundWindow;
        }

        // -----------------------------
        // Force window position until stable
        // -----------------------------
        public static async System.Threading.Tasks.Task ForceWindowPositionUntilStable(NoBS.Core.Profiles.WindowPosition app)
        {
            if (app == null || !app.ProcessId.HasValue)
                return;

            if (app.Width <= 0 || app.Height <= 0)
                return;

            const int maxAttempts = 30; // Try for up to 6 seconds (30 * 200ms)
            const int delayMs = 200;
            int stableCount = 0;
            const int requiredStableCount = 3; // Window must be stable for 3 consecutive checks

            Rectangle? lastRect = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(app.ProcessId.Value);
                    if (process.HasExited)
                        break;

                    IntPtr hWnd = process.MainWindowHandle;
                    if (hWnd == IntPtr.Zero)
                    {
                        // Try to find window by process ID if MainWindowHandle is not available
                        hWnd = FindWindowByProcessId(app.ProcessId.Value);
                    }

                    if (hWnd == IntPtr.Zero)
                    {
                        await System.Threading.Tasks.Task.Delay(delayMs);
                        continue;
                    }

                    // Update app's window handle
                    app.WindowHandle = hWnd;

                    // Get current window position
                    var currentRect = GetWindowRectSafe(hWnd);
                    if (!currentRect.HasValue)
                    {
                        await System.Threading.Tasks.Task.Delay(delayMs);
                        continue;
                    }

                    // Set desired position
                    SetWindowPosition(hWnd, app.X, app.Y, app.Width, app.Height);

                    // Check if window is at the desired position
                    await System.Threading.Tasks.Task.Delay(delayMs);

                    var newRect = GetWindowRectSafe(hWnd);
                    if (newRect.HasValue)
                    {
                        // Check if window is now at the correct position (with small tolerance)
                        bool isCorrect = Math.Abs(newRect.Value.X - app.X) <= 2 &&
                                        Math.Abs(newRect.Value.Y - app.Y) <= 2 &&
                                        Math.Abs(newRect.Value.Width - app.Width) <= 2 &&
                                        Math.Abs(newRect.Value.Height - app.Height) <= 2;

                        if (isCorrect)
                        {
                            stableCount++;
                            if (stableCount >= requiredStableCount)
                            {
                                // Window is stable at the correct position
                                break;
                            }
                        }
                        else
                        {
                            stableCount = 0;
                        }

                        lastRect = newRect;
                    }
                }
                catch
                {
                    // Process might have exited or other error occurred
                    break;
                }
            }
        }
    }
}
