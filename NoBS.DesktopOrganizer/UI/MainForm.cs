using NoBS.Core.Profiles;
using Microsoft.VisualBasic;
using NoBS.DesktopOrganizer.Core;
using NoBS.DesktopOrganizer.Core.Helpers;
using NoBS.DesktopOrganizer.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class MainForm : Form
    {
        private ListBox lstProfiles;
        private AnimatedButton btnCreateProfile;
        private AnimatedButton btnRename;
        private AnimatedButton btnDelete;

        private AnimatedButton btnSave;
        private AnimatedButton btnApply;
        private AnimatedButton btnKill;

        private Label lblOnlineStatus;
        private System.Windows.Forms.Timer statusBlinkTimer;

        private Panel rightPanel;
        private AppsEditorPanel appsEditor;
        private WallpaperEditorPanel wallpaperEditor;
        private SuggestionsPanel suggestionsPanel;
        private Label lblProfileTitle;
        private Label lblAppsOverlay;
        private Label lblStatusBar;
        private AnimatedButton btnToggleSuggestions;

        private readonly AppRunner appRunner = new();
        private readonly ProfileApplier profileApplier;
        private string? startupWallpaper;
        private NotifyIcon trayIcon;

        private TrackBar volumeSlider;
        private NumericUpDown volumeBox;
        private Label lblVolume;

        private WorkspaceProfile? currentProfile;
        private bool isProfileApplied = false;

        public MainForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;

            if (Theme.Background == null)
                throw new InvalidOperationException("Theme must be initialized before MainForm");

            Logger.ClearLog();
            Logger.LogInfo("=== NoBS Desktop Organizer Started ===");

            startupWallpaper = WallpaperHelper.GetCurrentWallpaper();
            if (!string.IsNullOrEmpty(startupWallpaper))
            {
                Logger.LogInfo($"Captured startup wallpaper: {startupWallpaper}");
            }

            profileApplier = new ProfileApplier(appRunner);

            InitializeComponent();
            LoadProfiles();
            LoadCustomIcon();
        }

        private void LoadCustomIcon()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("NoBS.DesktopOrganizer.icon.ico");
                if (stream != null)
                {
                    Icon = new Icon(stream);
                    Logger.LogInfo("Custom icon loaded successfully");
                }
                else
                {
                    Logger.LogWarning("Could not find embedded icon.ico resource");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to load custom icon: {ex.Message}");
            }
        }

        private Icon LoadTrayIcon()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("NoBS.DesktopOrganizer.icon.ico");
                if (stream != null)
                {
                    return new Icon(stream);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to load tray icon: {ex.Message}");
            }
            return SystemIcons.Application;
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "NoBS - Desktop Organizer";
            ClientSize = new Size(1000, 700);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = Theme.TextFont;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            KeyPreview = true;

            KeyDown += MainForm_KeyDown;
            FormClosing += MainForm_FormClosing;
            Paint += MainForm_Paint;

            // ============================
            // PROFILES LIST SECTION
            // ============================
            var lblProfilesTitle = new Label
            {
                Text = "▌PROFILES",
                Left = 15,
                Top = 12,
                Font = Theme.SectionFont,
                ForeColor = Theme.CrimsonBright,
                AutoSize = true
            };
            Controls.Add(lblProfilesTitle);

            lstProfiles = new ListBox
            {
                Left = 15,
                Top = 40,
                Width = 200,
                Height = ClientSize.Height - 210,
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 38,
                Font = Theme.TextFont
            };
            lstProfiles.DrawItem += DrawProfileItem;
            lstProfiles.SelectedIndexChanged += ProfileSelected;
            lstProfiles.MouseDown += LstProfiles_MouseDown;

            // Add border around list
            var profilesContainer = new Panel
            {
                Left = 13,
                Top = 38,
                Width = 204,
                Height = lstProfiles.Height + 4,
                BackColor = Color.Transparent
            };
            profilesContainer.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.BorderCrimson, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, profilesContainer.Width - 1, profilesContainer.Height - 1);
                }
            };
            Controls.Add(profilesContainer);
            profilesContainer.Controls.Add(lstProfiles);
            lstProfiles.Left = 2;
            lstProfiles.Top = 2;

            // ============================
            // PROFILE BUTTONS
            // ============================
            int buttonsTop = profilesContainer.Bottom + 8;

            btnCreateProfile = new AnimatedButton
            {
                Text = "+ CREATE",
                Left = 15,
                Top = buttonsTop,
                Width = 200,
                Height = 36
            };
            btnCreateProfile.Click += CreateProfile;
            Controls.Add(btnCreateProfile);

            btnDelete = new AnimatedButton
            {
                Text = "DELETE",
                Left = 15,
                Top = buttonsTop + 42,
                Width = 95,
                Height = 32,
                Enabled = false
            };
            btnDelete.FlatAppearance.BorderColor = Theme.Danger;
            btnDelete.ForeColor = Theme.Danger;
            btnDelete.Click += DeleteProfile;
            Controls.Add(btnDelete);

            btnRename = new AnimatedButton
            {
                Text = "RENAME",
                Left = 120,
                Top = buttonsTop + 42,
                Width = 95,
                Height = 32,
                Enabled = false
            };
            btnRename.Click += RenameProfile;
            Controls.Add(btnRename);

            // ============================
            // LINKS SECTION
            // ============================
            var skyBlue = Color.FromArgb(135, 206, 250);

            var linkGitHub = new LinkLabel
            {
                Text = "Visit my GitHub",
                Left = 60,
                Top = btnRename.Bottom + 15,
                AutoSize = true,
                LinkColor = skyBlue,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = skyBlue,
                Font = new Font("Segoe UI", 8.5f)
            };
            linkGitHub.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://GitHub.com/Secludedly",
                UseShellExecute = true
            });
            Controls.Add(linkGitHub);

            // Donation links - First row centered
            var linkKoFi = new LinkLabel
            {
                Text = "Ko-Fi",
                Left = 68,
                Top = linkGitHub.Bottom + 8,
                AutoSize = true,
                LinkColor = skyBlue,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = skyBlue,
                Font = new Font("Segoe UI", 8f)
            };
            linkKoFi.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://ko-fi.com/Secludedly",
                UseShellExecute = true
            });
            Controls.Add(linkKoFi);

            var lblPipe1 = new Label
            {
                Text = "|",
                Left = 108,
                Top = linkGitHub.Bottom + 8,
                AutoSize = true,
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 8f)
            };
            Controls.Add(lblPipe1);

            var linkPayPal = new LinkLabel
            {
                Text = "PayPal",
                Left = 118,
                Top = linkGitHub.Bottom + 8,
                AutoSize = true,
                LinkColor = skyBlue,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = skyBlue,
                Font = new Font("Segoe UI", 8f)
            };
            linkPayPal.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://bit.ly/3lOXg7l",
                UseShellExecute = true
            });
            Controls.Add(linkPayPal);

            // Second row centered
            var linkVenmo = new LinkLabel
            {
                Text = "Venmo",
                Left = 58,
                Top = linkKoFi.Bottom + 4,
                AutoSize = true,
                LinkColor = skyBlue,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = skyBlue,
                Font = new Font("Segoe UI", 8f)
            };
            linkVenmo.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://venmo.com/u/SecSteve",
                UseShellExecute = true
            });
            Controls.Add(linkVenmo);

            var lblPipe2 = new Label
            {
                Text = "|",
                Left = 106,
                Top = linkKoFi.Bottom + 4,
                AutoSize = true,
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 8f)
            };
            Controls.Add(lblPipe2);

            var linkCashApp = new LinkLabel
            {
                Text = "CashApp",
                Left = 116,
                Top = linkKoFi.Bottom + 4,
                AutoSize = true,
                LinkColor = skyBlue,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = skyBlue,
                Font = new Font("Segoe UI", 8f)
            };
            linkCashApp.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://cash.app/$Secludedly",
                UseShellExecute = true
            });
            Controls.Add(linkCashApp);

            // ============================
            // RIGHT PANEL
            // ============================
            rightPanel = new Panel
            {
                Left = 230,
                Top = 15,
                Width = ClientSize.Width - 245,
                Height = ClientSize.Height - 80,
                BackColor = Theme.DarkGray,
                AutoScroll = false
            };
            rightPanel.Paint += (s, e) =>
            {
                // Draw crimson border with shadow
                using (var shadowPen = new Pen(Theme.ShadowDark, 3))
                using (var borderPen = new Pen(Theme.BorderCrimson, 2))
                {
                    e.Graphics.DrawRectangle(shadowPen, 2, 2, rightPanel.Width - 3, rightPanel.Height - 3);
                    e.Graphics.DrawRectangle(borderPen, 0, 0, rightPanel.Width - 1, rightPanel.Height - 1);
                }
            };
            Controls.Add(rightPanel);

            lblProfileTitle = new Label
            {
                Text = "NO PROFILE SELECTED",
                Left = 20,
                Top = 18,
                Font = Theme.TitleFont,
                ForeColor = Theme.TextBright,
                AutoSize = true
            };
            rightPanel.Controls.Add(lblProfileTitle);

            // Online/Offline Status Indicator
            lblOnlineStatus = new Label
            {
                Text = "● OFFLINE",
                Left = lblProfileTitle.Right + 15,
                Top = 20,
                Font = Theme.StatusFont,
                ForeColor = Theme.StatusOffline,
                AutoSize = true,
                Visible = false
            };
            rightPanel.Controls.Add(lblOnlineStatus);

            // Status blink animation
            statusBlinkTimer = new System.Windows.Forms.Timer { Interval = 800 };
            statusBlinkTimer.Tick += (s, e) =>
            {
                if (lblOnlineStatus.Visible && isProfileApplied)
                {
                    lblOnlineStatus.ForeColor = lblOnlineStatus.ForeColor == Theme.StatusOnline
                        ? Color.FromArgb(20, 90, 20)
                        : Theme.StatusOnline;
                }
            };
            statusBlinkTimer.Start();

            appsEditor = new AppsEditorPanel(appRunner)
            {
                Left = 20,
                Top = 60,
                Width = rightPanel.Width - 60,
                Height = 220
            };
            rightPanel.Controls.Add(appsEditor);

            wallpaperEditor = new WallpaperEditorPanel
            {
                Left = 20,
                Top = 290,
                Width = rightPanel.Width - 60,
                Height = 180
            };
            rightPanel.Controls.Add(wallpaperEditor);

            // Suggestions Panel
            suggestionsPanel = new SuggestionsPanel
            {
                Left = 20,
                Top = 60,
                Width = rightPanel.Width - 40,
                Height = rightPanel.Height - 80,
                Visible = false
            };
            rightPanel.Controls.Add(suggestionsPanel);

            // Toggle button for suggestions
            btnToggleSuggestions = new AnimatedButton
            {
                Text = "SUGGESTIONS",
                Left = rightPanel.Width - 170,
                Top = 13,
                Width = 150,
                Height = 34
            };
            btnToggleSuggestions.Click += ToggleSuggestions;
            rightPanel.Controls.Add(btnToggleSuggestions);
            btnToggleSuggestions.BringToFront();

            lblAppsOverlay = new Label
            {
                Text = "Select a profile to edit apps",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 10f),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(160, Theme.Background),
                Visible = true
            };
            appsEditor.Controls.Add(lblAppsOverlay);
            lblAppsOverlay.BringToFront();

            SetAppsEditorEnabled(false);

            // ============================
            // SYSTEM VOLUME
            // ============================
            lblVolume = new Label
            {
                Text = "  ▌SYSTEM VOLUME",
                Left = 20,
                Top = wallpaperEditor.Bottom + 15,
                AutoSize = true,
                ForeColor = Theme.CrimsonBright,
                Font = Theme.SectionFont
            };
            rightPanel.Controls.Add(lblVolume);

            volumeSlider = new TrackBar
            {
                Left = 20,
                Top = lblVolume.Bottom + 6,
                Width = rightPanel.Width - 160,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                SmallChange = 1,
                LargeChange = 5
            };

            volumeBox = new NumericUpDown
            {
                Left = volumeSlider.Right + 10,
                Top = volumeSlider.Top,
                Width = 60,
                Minimum = 0,
                Maximum = 100
            };

            volumeSlider.Scroll += (_, __) =>
            {
                volumeBox.Value = volumeSlider.Value;
                AudioHelper.SetSystemVolume(volumeSlider.Value);
                if (currentProfile != null)
                {
                    currentProfile.SystemVolumePercent = volumeSlider.Value;
                    currentProfile.MarkDirty();
                }
            };

            volumeBox.ValueChanged += (_, __) =>
            {
                volumeSlider.Value = (int)volumeBox.Value;
                AudioHelper.SetSystemVolume((int)volumeBox.Value);
                if (currentProfile != null)
                {
                    currentProfile.SystemVolumePercent = (int)volumeBox.Value;
                    currentProfile.MarkDirty();
                }
            };

            rightPanel.Controls.Add(volumeSlider);
            rightPanel.Controls.Add(volumeBox);

            // ============================
            // BOTTOM BAR
            // ============================
            int bottomY = ClientSize.Height - 55;

            btnSave = new AnimatedButton
            {
                Text = "SAVE",
                Left = 235,
                Top = bottomY,
                Width = 90,
                Height = 38
            };
            Controls.Add(btnSave);

            btnApply = new AnimatedButton
            {
                Text = "APPLY",
                Left = 335,
                Top = bottomY,
                Width = 90,
                Height = 38,
                Enabled = false
            };
            Controls.Add(btnApply);

            btnKill = new AnimatedButton
            {
                Text = "KILL APPS",
                Left = 435,
                Top = bottomY,
                Width = 100,
                Height = 38,
                Enabled = false
            };
            btnKill.FlatAppearance.BorderColor = Theme.Danger;
            btnKill.ForeColor = Theme.Danger;
            Controls.Add(btnKill);

            // ============================
            // STATUS BAR
            // ============================
            lblStatusBar = new Label
            {
                Left = 225,
                Top = ClientSize.Height - 18,
                Width = ClientSize.Width - 280,
                Height = 20,
                Text = "SELECT PROFILE | Save: Ctrl+S | Apply: Ctrl+Enter | Refresh: F5",
                ForeColor = Theme.TextMuted,
                Font = Theme.StatusFont,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(lblStatusBar);

            // ============================
            // SYSTEM TRAY ICON
            // ============================
            trayIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Visible = false,
                Text = "NoBS Desktop Organizer"
            };
            trayIcon.DoubleClick += TrayIcon_DoubleClick;
            trayIcon.ContextMenuStrip = CreateTrayContextMenu();

            // Handle minimize to tray
            Resize += MainForm_Resize;

            // ============================
            // BUTTON LOGIC
            // ============================

            btnSave.Click += (_, __) =>
            {
                if (currentProfile == null)
                    return;

                int savedCount = 0;
                int notRunningCount = 0;

                appRunner.RefreshAppStatuses(currentProfile.Apps);

                foreach (var app in currentProfile.Apps)
                {
                    if (app.ProcessId.HasValue && app.WindowHandle != IntPtr.Zero)
                    {
                        var rect = WindowPositionHelper.GetWindowRectSafe(app.WindowHandle);
                        if (rect.HasValue)
                        {
                            app.X = rect.Value.X;
                            app.Y = rect.Value.Y;
                            app.Width = rect.Value.Width;
                            app.Height = rect.Value.Height;
                            savedCount++;
                        }
                    }
                    else
                    {
                        var hWnd = WindowPositionHelper.FindWindowByExecutable(app.ExecutablePath);
                        if (hWnd != IntPtr.Zero)
                        {
                            var rect = WindowPositionHelper.GetWindowRectSafe(hWnd);
                            if (rect.HasValue)
                            {
                                app.X = rect.Value.X;
                                app.Y = rect.Value.Y;
                                app.Width = rect.Value.Width;
                                app.Height = rect.Value.Height;
                                savedCount++;
                            }
                        }
                        else
                        {
                            notRunningCount++;
                        }
                    }
                }

                ProfileManager.SaveProfile(currentProfile);
                currentProfile.ClearDirty();

                string message = $"Profile saved.\n\nWindow positions saved: {savedCount}";
                if (notRunningCount > 0)
                {
                    message += $"\nApps not running (positions unchanged): {notRunningCount}";
                }

                MessageBox.Show(this, message, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnApply.Click += async (_, __) =>
            {
                if (currentProfile == null) return;

                btnApply.Enabled = false;
                UpdateStatusBar("Applying profile...");

                try
                {
                    await profileApplier.ApplyProfileAsync(currentProfile);

                    // Update online status
                    isProfileApplied = true;
                    lblOnlineStatus.Text = "● ONLINE";
                    lblOnlineStatus.ForeColor = Theme.StatusOnline;
                    lblOnlineStatus.Visible = true;

                    UpdateStatusBar($"Profile '{currentProfile.Name}' applied successfully!");
                    await Task.Delay(3000);
                    UpdateStatusBar("Ready");
                }
                catch (Exception ex)
                {
                    UpdateStatusBar("Error applying profile");
                    MessageBox.Show(this, ex.Message, "Apply Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnApply.Enabled = true;
                }
            };

            btnKill.Click += (_, __) =>
            {
                if (currentProfile == null) return;
                appRunner.KillAppsByProfile(currentProfile.Apps);

                // Update status to offline
                isProfileApplied = false;
                lblOnlineStatus.Text = "● OFFLINE";
                lblOnlineStatus.ForeColor = Color.FromArgb(180, 30, 30); // Red
            };

            ResumeLayout(false);
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            // Draw subtle vignette effect in corners
            using (var vignetteBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Point(0, 0),
                new Point(Width, Height),
                Color.FromArgb(15, 80, 15, 15),
                Color.FromArgb(0, 0, 0, 0)))
            {
                e.Graphics.FillRectangle(vignetteBrush, 0, 0, Width, 100);
            }
        }

        private void LoadProfiles()
        {
            lstProfiles.Items.Clear();
            foreach (var p in ProfileManager.LoadAllProfiles())
                lstProfiles.Items.Add(p);
            lstProfiles.DisplayMember = "Name";
        }

        private void ProfileSelected(object? sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem == null)
            {
                ClearProfile();
                return;
            }

            // Load the profile object from storage
            currentProfile = lstProfiles.SelectedItem as WorkspaceProfile;

            if (currentProfile == null)
            {
                ClearProfile();
                return;
            }

            // -------------------------------
            // REFRESH REAL APP STATUSES
            // -------------------------------
            appRunner.RefreshAppStatuses(currentProfile.Apps);

            // -------------------------------
            // Load profile into the editor panel
            // -------------------------------
            lblProfileTitle.Text = currentProfile.Name.ToUpper();
            appsEditor.LoadProfile(currentProfile);
            wallpaperEditor.LoadProfile(currentProfile);

            // Update online/offline status
            isProfileApplied = false;
            lblOnlineStatus.Visible = true;
            lblOnlineStatus.Left = lblProfileTitle.Right + 15;
            lblOnlineStatus.Text = "● OFFLINE";
            lblOnlineStatus.ForeColor = Color.FromArgb(180, 30, 30); // Red

            // -------------------------------
            // Refresh statuses immediately when switching profiles
            // -------------------------------
            appsEditor.RefreshStatuses();

            // -------------------------------
            // Enable buttons depending on app availability
            // -------------------------------
            btnRename.Enabled = true;
            btnDelete.Enabled = true;
            btnApply.Enabled = currentProfile.Apps.Any();
            btnKill.Enabled = currentProfile.Apps.Any();

            SetAppsEditorEnabled(true);

            // -------------------------------
            // Update status bar with app counts
            // -------------------------------
            int runningAppsStats = currentProfile.Apps.Count(a => a.Status == AppRunStatus.Running);
            int totalAppsStats = currentProfile.Apps.Count;
            UpdateStatusBar($"Profile: {currentProfile.Name} | Apps: {runningAppsStats}/{totalAppsStats} | Save: Ctrl+S | Apply: Ctrl+Enter | Refresh: F5");

            // -------------------------------
            // Load system volume
            // -------------------------------
            int currentVolume = AudioHelper.GetSystemVolume();
            int profileVolume = currentProfile.SystemVolumePercent ?? currentVolume;

            volumeSlider.Value = profileVolume;
            volumeBox.Value = profileVolume;
        }

        private void ClearProfile()
        {
            currentProfile = null;
            isProfileApplied = false;
            lblProfileTitle.Text = "NO PROFILE SELECTED";
            lblOnlineStatus.Visible = false;
            appsEditor.Clear();
            wallpaperEditor.Clear();
            volumeSlider.Value = AudioHelper.GetSystemVolume();
            volumeBox.Value = AudioHelper.GetSystemVolume();
            SetAppsEditorEnabled(false);
            btnApply.Enabled = false;
            btnKill.Enabled = false;
            btnRename.Enabled = false;
            btnDelete.Enabled = false;
            UpdateStatusBar("▌NO PROFILE SELECTED | Save: Ctrl+S | Apply: Ctrl+Enter | Refresh: F5");
        }

        private void SetAppsEditorEnabled(bool enabled)
        {
            appsEditor.Enabled = enabled;
            lblAppsOverlay.Visible = !enabled;
        }

        private void UpdateStatusBar(string message)
        {
            lblStatusBar.Text = message;
        }

        private void ToggleSuggestions(object? sender, EventArgs e)
        {
            if (suggestionsPanel.Visible)
            {
                // Switch back to profile editor
                suggestionsPanel.Visible = false;
                appsEditor.Visible = true;
                wallpaperEditor.Visible = true;
                lblVolume.Visible = true;
                volumeSlider.Visible = true;
                volumeBox.Visible = true;
                lblProfileTitle.Visible = true;
                lblOnlineStatus.Visible = currentProfile != null;
                btnToggleSuggestions.Text = "SUGGESTIONS";

                // Restore online/offline status based on applied state
                if (currentProfile != null)
                {
                    if (isProfileApplied)
                    {
                        lblOnlineStatus.Text = "● ONLINE";
                        lblOnlineStatus.ForeColor = Theme.StatusOnline;
                    }
                    else
                    {
                        lblOnlineStatus.Text = "● OFFLINE";
                        lblOnlineStatus.ForeColor = Color.FromArgb(180, 30, 30);
                    }
                }
            }
            else
            {
                // Switch to suggestions panel
                suggestionsPanel.Visible = true;
                appsEditor.Visible = false;
                wallpaperEditor.Visible = false;
                lblVolume.Visible = false;
                volumeSlider.Visible = false;
                volumeBox.Visible = false;
                lblProfileTitle.Visible = false;
                lblOnlineStatus.Visible = false;
                btnToggleSuggestions.Text = "BACK";
            }
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                btnSave.PerformClick();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Enter)
            {
                btnApply.PerformClick();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                LoadProfiles();
                e.Handled = true;
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Restore original wallpaper on exit
            if (!string.IsNullOrEmpty(startupWallpaper))
            {
                Logger.LogInfo($"Restoring startup wallpaper: {startupWallpaper}");
                bool success = WallpaperHelper.SetWallpaper(startupWallpaper);
                if (success)
                {
                    Logger.LogInfo("Startup wallpaper restored successfully");
                }
                else
                {
                    Logger.LogWarning("Failed to restore startup wallpaper");
                }
            }
            else
            {
                Logger.LogInfo("No startup wallpaper to restore");
            }

            // Cleanup tray icon
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(2000, "NoBS Desktop Organizer", "Minimized to tray. Double-click to restore.", ToolTipIcon.Info);
            }
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
            Activate();
        }

        private ContextMenuStrip CreateTrayContextMenu()
        {
            var menu = new ContextMenuStrip();

            // Show/Hide
            var showHideItem = new ToolStripMenuItem("Show Window");
            showHideItem.Click += (s, e) => TrayIcon_DoubleClick(s, e);
            showHideItem.Font = new Font(showHideItem.Font, FontStyle.Bold);
            menu.Items.Add(showHideItem);

            menu.Items.Add(new ToolStripSeparator());

            // Profiles submenu
            var profilesItem = new ToolStripMenuItem("Profiles");
            RefreshProfilesMenu(profilesItem);
            menu.Items.Add(profilesItem);

            menu.Items.Add(new ToolStripSeparator());

            // Kill All Apps
            var killAllItem = new ToolStripMenuItem("Kill All Profile Apps");
            killAllItem.Click += (s, e) =>
            {
                if (currentProfile != null)
                {
                    appRunner.KillAppsByProfile(currentProfile.Apps);
                    trayIcon.ShowBalloonTip(2000, "NoBS", $"Killed all apps for profile '{currentProfile.Name}'", ToolTipIcon.Info);
                }
            };
            menu.Items.Add(killAllItem);

            // Kill Profile & Exit
            var killAndExitItem = new ToolStripMenuItem("Kill Profile && Exit NoBS");
            killAndExitItem.ForeColor = Color.DarkRed;
            killAndExitItem.Click += (s, e) =>
            {
                var result = MessageBox.Show(
                    "This will kill all profile apps and exit NoBS Desktop Organizer.\n\nAre you sure?",
                    "Kill Profile & Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    if (currentProfile != null)
                    {
                        appRunner.KillAppsByProfile(currentProfile.Apps);
                    }
                    Application.Exit();
                }
            };
            menu.Items.Add(killAndExitItem);

            // Re-open Fresh
            var reopenFreshItem = new ToolStripMenuItem("Re-open All Apps Fresh");
            reopenFreshItem.Click += async (s, e) =>
            {
                if (currentProfile != null)
                {
                    await ReopenAllAppsFresh();
                    trayIcon.ShowBalloonTip(2000, "NoBS", $"Re-opened all apps fresh", ToolTipIcon.Info);
                }
            };
            menu.Items.Add(reopenFreshItem);

            menu.Items.Add(new ToolStripSeparator());

            // App Status submenu
            var statusItem = new ToolStripMenuItem("App Status");
            menu.Items.Add(statusItem);

            // Update status when menu opens
            menu.Opening += (s, e) =>
            {
                statusItem.DropDownItems.Clear();
                if (currentProfile != null && currentProfile.Apps.Any())
                {
                    appRunner.RefreshAppStatuses(currentProfile.Apps);
                    foreach (var app in currentProfile.Apps)
                    {
                        string status = app.Status == AppRunStatus.Running ? "● Running" : "○ Stopped";
                        var appStatusItem = new ToolStripMenuItem($"{status} - {app.Name}");
                        appStatusItem.Enabled = false;
                        statusItem.DropDownItems.Add(appStatusItem);
                    }
                }
                else
                {
                    var noAppsItem = new ToolStripMenuItem("No profile selected");
                    noAppsItem.Enabled = false;
                    statusItem.DropDownItems.Add(noAppsItem);
                }
            };

            menu.Items.Add(new ToolStripSeparator());

            // Restart Application
            var restartItem = new ToolStripMenuItem("Restart Application");
            restartItem.Click += (s, e) =>
            {
                Application.Restart();
            };
            menu.Items.Add(restartItem);

            // Exit
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) =>
            {
                Application.Exit();
            };
            menu.Items.Add(exitItem);

            return menu;
        }

        private void RefreshProfilesMenu(ToolStripMenuItem profilesMenuItem)
        {
            profilesMenuItem.DropDownItems.Clear();

            var profiles = ProfileManager.LoadAllProfiles();
            if (!profiles.Any())
            {
                var noProfilesItem = new ToolStripMenuItem("No profiles available");
                noProfilesItem.Enabled = false;
                profilesMenuItem.DropDownItems.Add(noProfilesItem);
                return;
            }

            foreach (var profile in profiles)
            {
                var profileItem = new ToolStripMenuItem(profile.Name);
                if (currentProfile?.Name == profile.Name)
                {
                    profileItem.Checked = true;
                }

                profileItem.Click += async (s, e) =>
                {
                    currentProfile = profile;
                    try
                    {
                        await profileApplier.ApplyProfileAsync(profile);
                        isProfileApplied = true;

                        // Update status if form is visible
                        if (lblOnlineStatus.Visible)
                        {
                            lblOnlineStatus.Text = "● ONLINE";
                            lblOnlineStatus.ForeColor = Theme.StatusOnline;
                        }

                        trayIcon.ShowBalloonTip(2000, "NoBS", $"Applied profile '{profile.Name}'", ToolTipIcon.Info);
                    }
                    catch (Exception ex)
                    {
                        trayIcon.ShowBalloonTip(2000, "Error", $"Failed to apply profile: {ex.Message}", ToolTipIcon.Error);
                    }
                };

                profilesMenuItem.DropDownItems.Add(profileItem);
            }
        }

        private async Task ReopenAllAppsFresh()
        {
            if (currentProfile == null) return;

            foreach (var app in currentProfile.Apps)
            {
                // Kill existing process
                if (app.ProcessId.HasValue)
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById(app.ProcessId.Value);
                        if (!process.HasExited)
                        {
                            process.Kill();
                            await Task.Delay(500);
                        }
                    }
                    catch { }
                }

                // Clear saved position
                app.X = 0;
                app.Y = 0;
                app.Width = 0;
                app.Height = 0;
                app.ProcessId = null;
                app.WindowHandle = IntPtr.Zero;
            }

            // Re-launch apps
            await appRunner.LaunchAppsAsync(currentProfile.Apps);
        }

        private void LstProfiles_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = lstProfiles.IndexFromPoint(e.Location);
                if (index >= 0 && index < lstProfiles.Items.Count)
                {
                    lstProfiles.SelectedIndex = index;
                    var profile = lstProfiles.Items[index] as WorkspaceProfile;
                    if (profile != null)
                    {
                        var contextMenu = CreateProfileContextMenu(profile);
                        contextMenu.Show(lstProfiles, e.Location);
                    }
                }
            }
        }

        private ContextMenuStrip CreateProfileContextMenu(WorkspaceProfile profile)
        {
            var menu = new ContextMenuStrip();

            // Apply Profile
            var applyItem = new ToolStripMenuItem("Apply Profile");
            applyItem.Font = new Font(applyItem.Font, FontStyle.Bold);
            applyItem.Click += async (s, e) =>
            {
                currentProfile = profile;
                lstProfiles.SelectedItem = profile;
                btnApply.PerformClick();
            };
            menu.Items.Add(applyItem);

            menu.Items.Add(new ToolStripSeparator());

            // Rename Profile
            var renameItem = new ToolStripMenuItem("Rename");
            renameItem.Click += (s, e) =>
            {
                btnRename.PerformClick();
            };
            menu.Items.Add(renameItem);

            // Kill All Apps
            var killAllItem = new ToolStripMenuItem("Kill All Apps");
            killAllItem.Click += (s, e) =>
            {
                appRunner.KillAppsByProfile(profile.Apps);
                MessageBox.Show(this, $"Killed all apps for profile '{profile.Name}'", "Apps Killed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            menu.Items.Add(killAllItem);

            menu.Items.Add(new ToolStripSeparator());

            // Delete Profile
            var deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.ForeColor = Color.DarkRed;
            deleteItem.Click += (s, e) =>
            {
                btnDelete.PerformClick();
            };
            menu.Items.Add(deleteItem);

            return menu;
        }

        private void DrawProfileItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstProfiles.Items.Count)
                return;

            var profile = lstProfiles.Items[e.Index] as WorkspaceProfile;
            if (profile == null) return;

            // Background
            Color bgColor = (e.State & DrawItemState.Selected) != 0
                ? Theme.CrimsonDark
                : Theme.DarkPanel;
            using (var bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            // Left crimson accent bar
            using (var accentBrush = new SolidBrush(Theme.CrimsonBright))
            {
                e.Graphics.FillRectangle(accentBrush, e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
            }

            // Profile name
            Color textColor = (e.State & DrawItemState.Selected) != 0
                ? Theme.TextBright
                : Theme.Text;
            using (var textBrush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(profile.Name, e.Font, textBrush, e.Bounds.X + 10, e.Bounds.Y + 8);
            }

            // Status indicator (small circle)
            int runningCount = profile.Apps.Count(a => a.Status == AppRunStatus.Running);
            Color statusColor = runningCount > 0 ? Theme.StatusOnline : Theme.StatusOffline;
            using (var statusBrush = new SolidBrush(statusColor))
            {
                e.Graphics.FillEllipse(statusBrush, e.Bounds.Right - 18, e.Bounds.Y + 14, 10, 10);
            }

            // Bottom border
            using (var borderPen = new Pen(Theme.BorderDark, 1))
            {
                e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        private void CreateProfile(object? sender, EventArgs e)
        {
            string name = Interaction.InputBox("Enter profile name:", "Create Profile", "");
            if (string.IsNullOrEmpty(name)) return;

            var profile = new WorkspaceProfile { Name = name };
            ProfileManager.SaveProfile(profile);
            LoadProfiles();
            lstProfiles.SelectedItem = lstProfiles.Items
                .OfType<WorkspaceProfile>()
                .FirstOrDefault(p => p.Name == name);
        }

        private void DeleteProfile(object? sender, EventArgs e)
        {
            if (currentProfile == null) return;

            if (MessageBox.Show(this, $"Are you sure you want to delete '{currentProfile.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ProfileManager.DeleteProfile(currentProfile.Name);
                LoadProfiles();
                ClearProfile();
            }
        }

        private void RenameProfile(object? sender, EventArgs e)
        {
            if (currentProfile == null) return;

            string? newName = Interaction.InputBox("Enter new profile name:", "Rename Profile", currentProfile.Name);
            if (string.IsNullOrEmpty(newName) || newName == currentProfile.Name) return;

            ProfileManager.RenameProfile(currentProfile.Name, newName);
            LoadProfiles();
            lstProfiles.SelectedItem = lstProfiles.Items
                .OfType<WorkspaceProfile>()
                .FirstOrDefault(p => p.Name == newName);
        }
    }
}
