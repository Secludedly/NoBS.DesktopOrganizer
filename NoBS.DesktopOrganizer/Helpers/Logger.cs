using System;
using System.IO;

namespace NoBS.DesktopOrganizer.Core.Helpers
{
    public static class Logger
    {
        private static readonly string LogFilePath;
        private static readonly object lockObject = new object();

        static Logger()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            LogFilePath = Path.Combine(appDir, "debug.log");
        }

        public static void Log(string message)
        {
            try
            {
                lock (lockObject)
                {
                    string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail if logging fails
            }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            string errorMessage = ex != null ? $"ERROR: {message} - {ex.Message}" : $"ERROR: {message}";
            Log(errorMessage);
        }

        public static void LogInfo(string message)
        {
            Log($"INFO: {message}");
        }

        public static void LogWarning(string message)
        {
            Log($"WARNING: {message}");
        }

        public static void ClearLog()
        {
            try
            {
                lock (lockObject)
                {
                    File.WriteAllText(LogFilePath, $"=== Log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
                }
            }
            catch
            {
                // Silently fail
            }
        }
    }
}
