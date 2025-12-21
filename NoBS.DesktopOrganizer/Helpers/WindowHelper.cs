using System;
using System.Runtime.InteropServices;

public static class WindowHelper
{
    private const int SW_MINIMIZE = 6;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int GWL_EXSTYLE = -20;

    public static void MinimizeAllUserWindows()
    {
        IntPtr shellWnd = GetShellWindow();

        EnumWindows((hwnd, lParam) =>
        {
            if (hwnd == shellWnd) return true; // skip desktop
            if (!IsWindowVisible(hwnd)) return true;

            // Skip tool windows (like taskbars, StartAllBack, widgets)
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

            // Minimizing only "normal" windows
            ShowWindow(hwnd, SW_MINIMIZE);
            return true;
        }, IntPtr.Zero);
    }
}
