using NoBS.Core.Profiles;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class SaveDetailsDialog : Form
    {
        private Panel contentPanel;
        private AnimatedButton btnOk;

        public SaveDetailsDialog(WorkspaceProfile profile, int savedCount, int notRunningCount)
        {
            InitializeComponent();
            BuildContent(profile, savedCount, notRunningCount);
        }

        private void InitializeComponent()
        {
            Text = "Profile Saved";
            ClientSize = new Size(600, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.TextFont;

            // Content panel (scrollable)
            contentPanel = new Panel
            {
                Left = 10,
                Top = 10,
                Width = ClientSize.Width - 20,
                Height = ClientSize.Height - 60,
                AutoScroll = true,
                BackColor = Theme.DarkPanel,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(contentPanel);

            // OK button
            btnOk = new AnimatedButton
            {
                Text = "OK",
                Left = (ClientSize.Width - 120) / 2,
                Top = ClientSize.Height - 45,
                Width = 120,
                Height = 35
            };
            btnOk.Click += (s, e) => DialogResult = DialogResult.OK;
            Controls.Add(btnOk);
        }

        private void BuildContent(WorkspaceProfile profile, int savedCount, int notRunningCount)
        {
            int yPos = 10;

            // Title
            var lblTitle = new Label
            {
                Text = $"Profile: {profile.Name}",
                Left = 10,
                Top = yPos,
                Width = contentPanel.Width - 20,
                Font = Theme.TitleFont,
                ForeColor = Theme.MidnightBright,
                AutoSize = false,
                Height = 25
            };
            contentPanel.Controls.Add(lblTitle);
            yPos += 30;

            // Summary
            var lblSummary = new Label
            {
                Text = $"Saved {savedCount} window positions\n" +
                       (notRunningCount > 0 ? $"{notRunningCount} apps not running (positions unchanged)" : ""),
                Left = 10,
                Top = yPos,
                Width = contentPanel.Width - 20,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = notRunningCount > 0 ? 40 : 20
            };
            contentPanel.Controls.Add(lblSummary);
            yPos += lblSummary.Height + 15;

            // Separator
            var separator1 = new Label
            {
                Text = new string('─', 80),
                Left = 10,
                Top = yPos,
                Width = contentPanel.Width - 20,
                ForeColor = Theme.BorderDark,
                AutoSize = false,
                Height = 15
            };
            contentPanel.Controls.Add(separator1);
            yPos += 20;

            // Applications list header
            var lblAppsHeader = new Label
            {
                Text = "▌APPLICATIONS",
                Left = 10,
                Top = yPos,
                Font = Theme.SectionFont,
                ForeColor = Theme.MidnightBright,
                AutoSize = true
            };
            contentPanel.Controls.Add(lblAppsHeader);
            yPos += 25;

            // List each app
            if (profile.Apps.Count > 0)
            {
                foreach (var app in profile.Apps)
                {
                    yPos = AddAppEntry(app, yPos);
                }
            }
            else
            {
                var lblNoApps = new Label
                {
                    Text = "No applications in this profile",
                    Left = 20,
                    Top = yPos,
                    Width = contentPanel.Width - 30,
                    ForeColor = Theme.TextMuted,
                    AutoSize = false,
                    Height = 20
                };
                contentPanel.Controls.Add(lblNoApps);
                yPos += 25;
            }

            // Profile settings
            yPos += 10;
            var separator2 = new Label
            {
                Text = new string('─', 80),
                Left = 10,
                Top = yPos,
                Width = contentPanel.Width - 20,
                ForeColor = Theme.BorderDark,
                AutoSize = false,
                Height = 15
            };
            contentPanel.Controls.Add(separator2);
            yPos += 20;

            var lblSettings = new Label
            {
                Text = "▌PROFILE SETTINGS",
                Left = 10,
                Top = yPos,
                Font = Theme.SectionFont,
                ForeColor = Theme.MidnightBright,
                AutoSize = true
            };
            contentPanel.Controls.Add(lblSettings);
            yPos += 25;

            // Volume
            var lblVolume = new Label
            {
                Text = $"System Volume: {profile.SystemVolumePercent ?? 100}%",
                Left = 20,
                Top = yPos,
                Width = contentPanel.Width - 30,
                ForeColor = Theme.Text,
                AutoSize = false,
                Height = 20
            };
            contentPanel.Controls.Add(lblVolume);
            yPos += 22;

            // Wallpaper
            if (!string.IsNullOrEmpty(profile.WallpaperPath))
            {
                var lblWallpaper = new Label
                {
                    Text = $"Wallpaper: {System.IO.Path.GetFileName(profile.WallpaperPath)}",
                    Left = 20,
                    Top = yPos,
                    Width = contentPanel.Width - 30,
                    ForeColor = Theme.Text,
                    AutoSize = false,
                    Height = 20
                };
                contentPanel.Controls.Add(lblWallpaper);
                yPos += 22;
            }

            // Virtual Desktop
            if (!string.IsNullOrEmpty(profile.VirtualDesktopId))
            {
                var lblVirtualDesktop = new Label
                {
                    Text = $"Virtual Desktop: {profile.VirtualDesktopId}",
                    Left = 20,
                    Top = yPos,
                    Width = contentPanel.Width - 30,
                    ForeColor = Theme.Text,
                    AutoSize = false,
                    Height = 20
                };
                contentPanel.Controls.Add(lblVirtualDesktop);
                yPos += 22;

                if (profile.RenameVirtualDesktop)
                {
                    var lblRename = new Label
                    {
                        Text = "  → Rename desktop to profile name on apply",
                        Left = 20,
                        Top = yPos,
                        Width = contentPanel.Width - 30,
                        ForeColor = Theme.TextMuted,
                        AutoSize = false,
                        Height = 20
                    };
                    contentPanel.Controls.Add(lblRename);
                    yPos += 22;
                }
            }
        }

        private int AddAppEntry(WindowPosition app, int yPos)
        {
            // App name
            var lblName = new Label
            {
                Text = $"● {app.Name}",
                Left = 20,
                Top = yPos,
                Width = contentPanel.Width - 30,
                Font = new Font(Theme.TextFont.FontFamily, Theme.TextFont.Size, FontStyle.Bold),
                ForeColor = Theme.TextBright,
                AutoSize = false,
                Height = 20
            };
            contentPanel.Controls.Add(lblName);
            yPos += 22;

            // Details
            var details = new StringBuilder();
            details.Append($"  Position: ({app.X}, {app.Y})");
            if (app.Width > 0 && app.Height > 0)
                details.Append($"  |  Size: {app.Width}x{app.Height}");

            if (app.LaunchDelaySeconds > 0)
                details.Append($"  |  Delay: {app.LaunchDelaySeconds}s");

            if (!string.IsNullOrEmpty(app.AssignedMonitorDeviceName))
                details.Append($"  |  Monitor: {app.AssignedMonitorDeviceName}");

            var lblDetails = new Label
            {
                Text = details.ToString(),
                Left = 20,
                Top = yPos,
                Width = contentPanel.Width - 30,
                ForeColor = Theme.TextMuted,
                Font = new Font(Theme.TextFont.FontFamily, 8.5f),
                AutoSize = false,
                Height = 18
            };
            contentPanel.Controls.Add(lblDetails);
            yPos += 20;

            // Path
            var lblPath = new Label
            {
                Text = $"  {app.ExecutablePath}",
                Left = 20,
                Top = yPos,
                Width = contentPanel.Width - 30,
                ForeColor = Theme.TextMuted,
                Font = new Font(Theme.TextFont.FontFamily, 8f),
                AutoSize = false,
                Height = 18
            };
            contentPanel.Controls.Add(lblPath);
            yPos += 25;

            return yPos;
        }
    }
}
