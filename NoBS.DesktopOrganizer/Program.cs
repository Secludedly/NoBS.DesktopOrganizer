using System;
using System.Windows.Forms;
using NoBS.DesktopOrganizer.UI;

namespace NoBS.DesktopOrganizer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.Run(new MainForm());
        }
    }
}
