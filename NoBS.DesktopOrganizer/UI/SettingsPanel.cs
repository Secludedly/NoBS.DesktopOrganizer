using NoBS.DesktopOrganizer.Core.Settings;
using NoBS.Core.Profiles;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class SettingsPanel : Panel
    {
        private CheckBox chkStartWithWindows;
        private CheckBox chkStartMinimizedTaskbar;
        private CheckBox chkStartMinimizedTray;
        private ComboBox cboStartupProfile;
        private CheckBox chkDisableVolume;
        private CheckBox chkDisableWallpaper;
        private CheckBox chkDisableVirtualDesktop;
        private CheckBox chkDisableLiveTracking;
        private ComboBox cboMinimizeAfterApply;

        private AnimatedButton btnSave;
        private AnimatedButton btnCancel;

        private bool isLoading;
        private bool isBuilt;

        public event EventHandler? SettingsClosed;

        public SettingsPanel()
        {
            BackColor = Theme.Background;
            AutoScroll = true;

            Paint += (_, e) =>
            {
                using (var pen = new Pen(Theme.BorderMidnight, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
                using (var shadowPen = new Pen(Theme.ShadowDark, 1))
                {
                    e.Graphics.DrawRectangle(shadowPen, 1, 1, Width - 3, Height - 3);
                }
            };

            HandleCreated += (s, e) =>
            {
                if (!isBuilt) BuildUI();
            };
        }

        private void BuildUI()
        {
            if (isBuilt) return;  // Only build once

            isBuilt = true;

            int yPos = 10;
            int leftMargin = 20;
            int labelWidth = Width - 60;  // More padding to prevent cutoff

            // Title
            var lblTitle = new Label
            {
                Text = "SETTINGS",
                Left = leftMargin,
                Top = yPos,
                Font = Theme.TitleFont,
                ForeColor = Theme.MidnightBright,
                AutoSize = true
            };
            Controls.Add(lblTitle);
            yPos += 35;

            // ==========================================
            // STARTUP SETTINGS
            // ==========================================
            yPos = AddCategoryHeader("STARTUP", yPos, leftMargin);

            chkStartWithWindows = new CheckBox
            {
                Text = "Start with Windows",
                Left = leftMargin + 10,
                Top = yPos,
                Width = labelWidth,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 25
            };
            Controls.Add(chkStartWithWindows);
            yPos += 30;

            chkStartMinimizedTaskbar = new CheckBox
            {
                Text = "Start minimized to Taskbar",
                Left = leftMargin + 10,
                Top = yPos,
                Width = labelWidth,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 25
            };
            chkStartMinimizedTaskbar.CheckedChanged += (s, e) =>
            {
                if (!isLoading && chkStartMinimizedTaskbar.Checked)
                    chkStartMinimizedTray.Checked = false;
            };
            Controls.Add(chkStartMinimizedTaskbar);
            yPos += 30;

            chkStartMinimizedTray = new CheckBox
            {
                Text = "Start minimized to Tray",
                Left = leftMargin + 10,
                Top = yPos,
                Width = labelWidth,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 25
            };
            chkStartMinimizedTray.CheckedChanged += (s, e) =>
            {
                if (!isLoading && chkStartMinimizedTray.Checked)
                    chkStartMinimizedTaskbar.Checked = false;
            };
            Controls.Add(chkStartMinimizedTray);
            yPos += 30;

            var lblStartProfile = new Label
            {
                Text = "Start profile on program start:",
                Left = leftMargin + 10,
                Top = yPos,
                Width = labelWidth,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 20
            };
            Controls.Add(lblStartProfile);
            yPos += 22;

            cboStartupProfile = new ComboBox
            {
                Left = leftMargin + 10,
                Top = yPos,
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList,
                ForeColor = Theme.Text,
                BackColor = Theme.Panel,
                Font = Theme.TextFont,
                FlatStyle = FlatStyle.Flat
            };
            Controls.Add(cboStartupProfile);
            yPos += 40;

            // ==========================================
            // FEATURE TOGGLES
            // ==========================================
            yPos = AddCategoryHeader("FEATURE TOGGLES", yPos, leftMargin);

            chkDisableVolume = new CheckBox
            {
                Text = "Disable system volume modifications",
                Left = leftMargin + 10,
                Top = yPos,
                Width = labelWidth,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 25
            };
            Controls.Add(chkDisableVolume);
            yPos += 30;

            chkDisableWallpaper = new CheckBox
            {
                Text = "Disable wallpaper modifications",
                Left = leftMargin + 10,
                Top = yPos,
                Width = labelWidth,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 25
            };
            Controls.Add(chkDisableWallpaper);
            yPos += 30;

            chkDisableVirtualDesktop = new CheckBox
            {
                Text = "Disable virtual desktop support",
                Left = leftMargin + 10,
                Top = yPos,
                Width = labelWidth,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 25
            };
            Controls.Add(chkDisableVirtualDesktop);
            yPos += 30;

            chkDisableLiveTracking = new CheckBox
            {
                Text = "Disable live tracking of windows",
                Left = leftMargin + 10,
                Top = yPos,
                Width = labelWidth,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 25
            };
            Controls.Add(chkDisableLiveTracking);
            yPos += 40;

            // ==========================================
            // BEHAVIOR SETTINGS
            // ==========================================
            yPos = AddCategoryHeader("BEHAVIOR", yPos, leftMargin);

            var lblMinimize = new Label
            {
                Text = "Minimize after applying profile:",
                Left = leftMargin + 10,
                Top = yPos,
                Width = labelWidth,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 20
            };
            Controls.Add(lblMinimize);
            yPos += 22;

            cboMinimizeAfterApply = new ComboBox
            {
                Left = leftMargin + 10,
                Top = yPos,
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList,
                ForeColor = Theme.Text,
                BackColor = Theme.Panel,
                Font = Theme.TextFont,
                FlatStyle = FlatStyle.Flat
            };
            cboMinimizeAfterApply.Items.AddRange(new object[]
            {
                "Ask every time",
                "Minimize to Tray",
                "Minimize to Taskbar",
                "Do not minimize"
            });
            Controls.Add(cboMinimizeAfterApply);
            yPos += 50;

            // ==========================================
            // BUTTONS
            // ==========================================
            btnSave = new AnimatedButton
            {
                Text = "SAVE",
                Left = leftMargin + 10,
                Top = yPos,
                Width = 120,
                Height = 38
            };
            btnSave.Click += SaveSettings;
            Controls.Add(btnSave);

            btnCancel = new AnimatedButton
            {
                Text = "CANCEL",
                Left = leftMargin + 140,
                Top = yPos,
                Width = 120,
                Height = 38
            };
            btnCancel.Click += (s, e) => SettingsClosed?.Invoke(this, EventArgs.Empty);
            Controls.Add(btnCancel);
        }

        private int AddCategoryHeader(string text, int yPos, int leftMargin)
        {
            var lbl = new Label
            {
                Text = $"▌{text}",
                Left = leftMargin,
                Top = yPos,
                Font = Theme.SectionFont,
                ForeColor = Theme.MidnightBright,
                AutoSize = true
            };
            Controls.Add(lbl);
            return yPos + 28;
        }

        public void LoadSettings()
        {
            // Ensure UI is built before loading settings
            if (!isBuilt)
            {
                BuildUI();
            }

            isLoading = true;

            var settings = SettingsManager.CurrentSettings;

            chkStartWithWindows.Checked = SettingsManager.IsSetToStartWithWindows();
            chkStartMinimizedTaskbar.Checked = settings.StartMinimizedToTaskbar;
            chkStartMinimizedTray.Checked = settings.StartMinimizedToTray;

            chkDisableVolume.Checked = settings.DisableSystemVolumeModifications;
            chkDisableWallpaper.Checked = settings.DisableWallpaperModifications;
            chkDisableVirtualDesktop.Checked = settings.DisableVirtualDesktopSupport;
            chkDisableLiveTracking.Checked = settings.DisableLiveWindowTracking;

            cboMinimizeAfterApply.SelectedIndex = (int)settings.MinimizeAfterApply;

            // Load startup profiles
            cboStartupProfile.Items.Clear();
            cboStartupProfile.Items.Add("(None)");

            var profiles = ProfileManager.LoadAllProfiles();
            foreach (var profile in profiles)
            {
                cboStartupProfile.Items.Add(profile.Name);
            }

            if (!string.IsNullOrEmpty(settings.StartupProfileName))
            {
                int index = cboStartupProfile.Items.IndexOf(settings.StartupProfileName);
                cboStartupProfile.SelectedIndex = index >= 0 ? index : 0;
            }
            else
            {
                cboStartupProfile.SelectedIndex = 0;
            }

            isLoading = false;
        }

        private void SaveSettings(object? sender, EventArgs e)
        {
            var settings = SettingsManager.CurrentSettings;

            settings.StartMinimizedToTaskbar = chkStartMinimizedTaskbar.Checked;
            settings.StartMinimizedToTray = chkStartMinimizedTray.Checked;

            settings.DisableSystemVolumeModifications = chkDisableVolume.Checked;
            settings.DisableWallpaperModifications = chkDisableWallpaper.Checked;
            settings.DisableVirtualDesktopSupport = chkDisableVirtualDesktop.Checked;
            settings.DisableLiveWindowTracking = chkDisableLiveTracking.Checked;

            settings.MinimizeAfterApply = (MinimizeAfterApplyBehavior)cboMinimizeAfterApply.SelectedIndex;

            // Startup profile
            if (cboStartupProfile.SelectedIndex > 0)
            {
                settings.StartupProfileName = cboStartupProfile.SelectedItem.ToString();
            }
            else
            {
                settings.StartupProfileName = null;
            }

            // Start with Windows (registry)
            SettingsManager.SetStartWithWindows(chkStartWithWindows.Checked);

            SettingsManager.Save();

            MessageBox.Show(this, "Settings saved successfully.\n\nSome settings will take effect on next restart.",
                "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

            SettingsClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
