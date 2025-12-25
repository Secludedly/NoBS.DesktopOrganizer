using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using NoBS.DesktopOrganizer.Core.Helpers;

namespace NoBS.DesktopOrganizer.Core.Settings
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private static AppSettings? _currentSettings;

        public static AppSettings CurrentSettings
        {
            get
            {
                if (_currentSettings == null)
                    Load();
                return _currentSettings!;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    _currentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    Logger.LogInfo("Settings loaded successfully");
                }
                else
                {
                    _currentSettings = new AppSettings();
                    Save(); // Create default settings file
                    Logger.LogInfo("Created default settings file");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load settings", ex);
                _currentSettings = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsFilePath, json);
                Logger.LogInfo("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to save settings", ex);
            }
        }

        // Registry helpers for "Start with Windows"
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "NoBS.DesktopOrganizer";

        public static void SetStartWithWindows(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
                if (key == null)
                {
                    Logger.LogError("Failed to open registry key for startup");
                    return;
                }

                if (enable)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                        Logger.LogInfo($"Added to Windows startup: {exePath}");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    Logger.LogInfo("Removed from Windows startup");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to set Start with Windows", ex);
            }
        }

        public static bool IsSetToStartWithWindows()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to check if set to start with Windows", ex);
                return false;
            }
        }
    }
}
