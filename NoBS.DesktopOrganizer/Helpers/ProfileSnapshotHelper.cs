using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using NoBS.Core.Profiles;

namespace NoBS.DesktopOrganizer.Core.Helpers
{
    public static class ProfileSnapshotHelper
    {
        public static void CaptureCurrentWindows(WorkspaceProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            profile.Apps.Clear();

            var shellWindow = GetShellWindow();

            EnumWindows((hWnd, lParam) =>
            {
                if (hWnd == shellWindow)
                    return true;

                if (!IsWindowVisible(hWnd))
                    return true;

                if (IsToolWindow(hWnd))
                    return true;

                if (!GetWindowRect(hWnd, out RECT rect))
                    return true;

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (width <= 100 || height <= 100)
                    return true;

                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);

                Process? process = null;
                try
                {
                    process = Process.GetProcessById((int)processId);
                }
                catch
                {
                    return true;
                }

                if (process.MainModule == null)
                    return true;

                var app = new WindowPosition
                {
                    Name = process.ProcessName,
                    ExecutablePath = process.MainModule.FileName,
                    ProcessId = process.Id,
                    WindowHandle = hWnd,
                    X = rect.Left,
                    Y = rect.Top,
                    Width = width,
                    Height = height,
                    Status = AppRunStatus.Running,
                    KillOnSwitch = false
                };

                profile.Apps.Add(app);
                return true;

            }, IntPtr.Zero);
        }

        public static List<WindowPosition> GetCurrentWindows()
        {
            var windows = new List<WindowPosition>();
            var shellWindow = GetShellWindow();

            EnumWindows((hWnd, lParam) =>
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;
                if (IsToolWindow(hWnd)) return true;
                if (!GetWindowRect(hWnd, out RECT rect)) return true;

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width <= 100 || height <= 100) return true;

                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);

                Process? process = null;
                try { process = Process.GetProcessById((int)processId); }
                catch { return true; }
                if (process?.MainModule == null) return true;

                windows.Add(new WindowPosition
                {
                    Name = process.ProcessName,
                    ExecutablePath = process.MainModule.FileName,
                    ProcessId = process.Id,
                    WindowHandle = hWnd,
                    X = rect.Left,
                    Y = rect.Top,
                    Width = width,
                    Height = height,
                    Status = AppRunStatus.Running,
                    KillOnSwitch = false
                });

                return true;

            }, IntPtr.Zero);

            return windows;
        }

        public static List<WindowPosition> ShowWindowSelectionDialog(List<WindowPosition> windows)
        {
            using var form = new Form()
            {
                Width = 400,
                Height = 600,
                Text = "Select windows to include",
                StartPosition = FormStartPosition.CenterParent
            };

            var checkedList = new CheckedListBox()
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true
            };

            // Populate the list
            foreach (var w in windows)
                checkedList.Items.Add($"{w.Name} ({w.ExecutablePath})", true);

            var btnOk = new Button()
            {
                Text = "OK",
                Dock = DockStyle.Bottom,
                Height = 40,
                DialogResult = DialogResult.OK
            };

            form.Controls.Add(checkedList);
            form.Controls.Add(btnOk);
            form.AcceptButton = btnOk;

            var result = form.ShowDialog();

            if (result != DialogResult.OK) return new List<WindowPosition>();

            // Return only checked items
            var selected = new List<WindowPosition>();
            for (int i = 0; i < checkedList.Items.Count; i++)
            {
                if (checkedList.GetItemChecked(i))
                    selected.Add(windows[i]);
            }

            return selected;
        }

        // =============================
        // Win32
        // =============================

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;

        private static bool IsToolWindow(IntPtr hWnd)
        {
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            return (exStyle & WS_EX_TOOLWINDOW) != 0;
        }

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
