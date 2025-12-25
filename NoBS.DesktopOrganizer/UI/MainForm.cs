using NoBS.Core.Profiles;
using Microsoft.VisualBasic;
using NoBS.DesktopOrganizer.Core;
using NoBS.DesktopOrganizer.Core.Helpers;
using NoBS.DesktopOrganizer.Core.Settings;
using NoBS.DesktopOrganizer.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class MainForm : Form
    {
        private ListBox lstProfiles;
        private AnimatedButton btnCreateProfile;
        private AnimatedButton btnRename;
        private AnimatedButton btnDelete;
        private AnimatedButton btnMoveProfileUp;
        private AnimatedButton btnMoveProfileDown;

        private AnimatedButton btnSave;
        private AnimatedButton btnApply;
        private AnimatedButton btnKill;

        private Label lblOnlineStatus;
        private System.Windows.Forms.Timer statusBlinkTimer;

        private Panel rightPanel;
        private AppsEditorPanel appsEditor;
        private WallpaperEditorPanel wallpaperEditor;
        private SuggestionsPanel suggestionsPanel;
        private SettingsPanel settingsPanel;
        private Label lblProfileTitle;
        private Label lblAppsOverlay;
        private Label lblStatusBar;
        private AnimatedButton btnToggleSuggestions;
        private AnimatedButton btnMinimizeToTray;
        private AnimatedButton btnSettings;

        private readonly AppRunner appRunner = new();
        private readonly ProfileApplier profileApplier;
        private string? startupWallpaper;
        private NotifyIcon trayIcon;

        private TrackBar volumeSlider;
        private NumericUpDown volumeBox;
        private Label lblVolume;

        private Label lblVirtualDesktop;
        private ComboBox cboVirtualDesktop;
        private CheckBox chkRenameDesktop;

        private WorkspaceProfile? currentProfile;
        private bool isProfileApplied = false;
        private bool isLoadingProfile = false;

        public MainForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;

            if (Theme.Background == null)
                throw new InvalidOperationException("Theme must be initialized before MainForm");

            // Load settings early
            SettingsManager.Load();

            Logger.ClearLog();
            Logger.LogInfo("=== NoBS Desktop Organizer Started ===");

            startupWallpaper = WallpaperHelper.GetCurrentWallpaper();
            if (!string.IsNullOrEmpty(startupWallpaper))
            {
                Logger.LogInfo($"Captured startup wallpaper: {startupWallpaper}");
            }

            profileApplier = new ProfileApplier(appRunner);

            InitializeComponent();

            // Apply settings to UI
            ApplySettingsToUI();

            // Handle startup minimized behavior
            var settings = SettingsManager.CurrentSettings;
            if (settings.StartMinimizedToTray)
            {
                WindowState = FormWindowState.Minimized;
                Hide();
                trayIcon.Visible = true;
            }
            else if (settings.StartMinimizedToTaskbar)
            {
                WindowState = FormWindowState.Minimized;
            }

            LoadProfiles();
            LoadCustomIcon();

            // Handle startup profile after a brief delay
            if (!string.IsNullOrEmpty(settings.StartupProfileName))
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000); // Wait for UI to fully load
                    var profile = ProfileManager.LoadAllProfiles()
                        .FirstOrDefault(p => p.Name == settings.StartupProfileName);
                    if (profile != null)
                    {
                        Invoke(new Action(() =>
                        {
                            currentProfile = profile;
                            lstProfiles.SelectedItem = profile;
                        }));
                        await Task.Delay(500);
                        await profileApplier.ApplyProfileAsync(profile);
                        Invoke(new Action(() =>
                        {
                            isProfileApplied = true;
                            lblOnlineStatus.Text = "● ONLINE";
                            lblOnlineStatus.ForeColor = Theme.StatusOnline;
                            lblOnlineStatus.Visible = true;
                        }));
                    }
                });
            }
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

            Text = $"NoBS - Desktop Organizer (v{AppVersion.Current})";
            ClientSize = new Size(1000, 800);
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
                ForeColor = Theme.MidnightBright,
                AutoSize = true
            };
            Controls.Add(lblProfilesTitle);

            lstProfiles = new ListBox
            {
                Left = 15,
                Top = 40,
                Width = 200,
                Height = ClientSize.Height - 250,  // Reduced by 40 to make room for Up/Down buttons
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
                using (var pen = new Pen(Theme.BorderMidnight, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, profilesContainer.Width - 1, profilesContainer.Height - 1);
                }
            };
            Controls.Add(profilesContainer);
            profilesContainer.Controls.Add(lstProfiles);
            lstProfiles.Left = 2;
            lstProfiles.Top = 2;

            // ============================
            // Profile reordering buttons
            // ============================
            var profileButtonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.Transparent,
                ColumnCount = 2,
                RowCount = 1
            };

            // Each button gets 50% width
            profileButtonsPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50f));
            profileButtonsPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50f));

            profileButtonsPanel.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100f));

            profilesContainer.Controls.Add(profileButtonsPanel);

            // ---- UP BUTTON ----
            btnMoveProfileUp = new AnimatedButton
            {
                Text = "↑ UP",
                Anchor = AnchorStyles.None,  // Center in cell
                Width = 95,
                Height = 32,
                Enabled = false
            };
            btnMoveProfileUp.Click += MoveProfileUp;

            // ---- DOWN BUTTON ----
            btnMoveProfileDown = new AnimatedButton
            {
                Text = "↓ DOWN",
                Anchor = AnchorStyles.None,  // Center in cell
                Width = 95,
                Height = 32,
                Enabled = false
            };
            btnMoveProfileDown.Click += MoveProfileDown;

            // Add buttons side-by-side
            profileButtonsPanel.Controls.Add(btnMoveProfileUp, 0, 0);
            profileButtonsPanel.Controls.Add(btnMoveProfileDown, 1, 0);

            // Resize ListBox so it doesn't overlap the buttons
            lstProfiles.Height = profilesContainer.Height
                - profileButtonsPanel.Height
                - 4;

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

            // Donation links
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

            // Second row
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
                AutoScroll = true
            };
            rightPanel.Paint += (s, e) =>
            {
                // Draw crimson border with shadow
                using (var shadowPen = new Pen(Theme.ShadowDark, 3))
                using (var borderPen = new Pen(Theme.BorderMidnight, 2))
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

                // Refresh profile list icons to show live status updates
                var allProfiles = lstProfiles.Items.Cast<WorkspaceProfile>().ToList();
                foreach (var profile in allProfiles)
                {
                    appRunner.RefreshAppStatuses(profile.Apps);
                }
                lstProfiles.Invalidate(); // Force redraw with updated statuses
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

            // Settings panel
            settingsPanel = new SettingsPanel
            {
                Left = 20,
                Top = 60,
                Width = rightPanel.Width - 40,
                Height = rightPanel.Height - 80,
                Visible = false
            };
            settingsPanel.SettingsClosed += (s, e) => ToggleSettings(null, EventArgs.Empty);
            rightPanel.Controls.Add(settingsPanel);

            // Minimize to Tray button
            btnMinimizeToTray = new AnimatedButton
            {
                Text = "MINIMIZE TO TRAY",
                Left = rightPanel.Width - 200,
                Top = 13,
                Width = 180,
                Height = 34
            };
            btnMinimizeToTray.Click += (s, e) =>
            {
                Hide();
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(2000, "NoBS", "Minimized to tray. Double-click to restore.", ToolTipIcon.Info);
            };
            rightPanel.Controls.Add(btnMinimizeToTray);
            btnMinimizeToTray.BringToFront();

            // Settings button
            btnSettings = new AnimatedButton
            {
                Text = "SETTINGS",
                Left = rightPanel.Width - 390,
                Top = 13,
                Width = 180,
                Height = 34
            };
            btnSettings.Click += ToggleSettings;
            rightPanel.Controls.Add(btnSettings);
            btnSettings.BringToFront();

            // Suggestions button (bottom right corner of form)
            btnToggleSuggestions = new AnimatedButton
            {
                Text = "SUGGESTIONS",
                Width = 150,
                Height = 35,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnToggleSuggestions.Left = ClientSize.Width - btnToggleSuggestions.Width - 15;
            btnToggleSuggestions.Top = ClientSize.Height - btnToggleSuggestions.Height - 15;
            btnToggleSuggestions.Click += ToggleSuggestions;
            Controls.Add(btnToggleSuggestions);
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
                ForeColor = Theme.MidnightBright,
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

                // Only update the profile's stored volume, do NOT change system volume
                // System volume will only be applied when the "Apply" button is clicked
                if (!isLoadingProfile && currentProfile != null)
                {
                    currentProfile.SystemVolumePercent = volumeSlider.Value;
                    currentProfile.MarkDirty();
                }
            };

            volumeBox.ValueChanged += (_, __) =>
            {
                volumeSlider.Value = (int)volumeBox.Value;

                // Only update the profile's stored volume, do NOT change system volume
                // System volume will only be applied when the "Apply" button is clicked
                if (!isLoadingProfile && currentProfile != null)
                {
                    currentProfile.SystemVolumePercent = (int)volumeBox.Value;
                    currentProfile.MarkDirty();
                }
            };

            rightPanel.Controls.Add(volumeSlider);
            rightPanel.Controls.Add(volumeBox);

            // Set default volume to 30 on program load
            int defaultVolume = 30;
            volumeSlider.Value = defaultVolume;
            volumeBox.Value = defaultVolume;

            // Immediately apply system volume
            AudioHelper.SetSystemVolume(defaultVolume);

            // ============================
            // VIRTUAL DESKTOP
            // ============================
            lblVirtualDesktop = new Label
            {
                Text = "  ▌VIRTUAL DESKTOP",
                Left = 20,
                Top = volumeSlider.Bottom + 15,
                AutoSize = true,
                ForeColor = Theme.MidnightBright,
                Font = Theme.SectionFont,
                Visible = true
            };
            rightPanel.Controls.Add(lblVirtualDesktop);

            cboVirtualDesktop = new ComboBox
            {
                Left = 20,
                Top = lblVirtualDesktop.Bottom + 6,
                Width = rightPanel.Width - 40,
                DropDownStyle = ComboBoxStyle.DropDownList,
                ForeColor = Theme.Text,
                BackColor = Theme.Panel,
                Font = Theme.TextFont,
                FlatStyle = FlatStyle.Flat,
                Visible = true
            };
            rightPanel.Controls.Add(cboVirtualDesktop);

            chkRenameDesktop = new CheckBox
            {
                Text = "Rename virtual desktop to profile name on apply",
                Left = 20,
                Top = cboVirtualDesktop.Bottom + 8,
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.TextFont,
                Visible = true
            };
            rightPanel.Controls.Add(chkRenameDesktop);

            // Event handlers for Virtual Desktop controls
            cboVirtualDesktop.SelectedIndexChanged += (_, __) =>
            {
                if (!isLoadingProfile && currentProfile != null)
                {
                    var selectedDesktop = cboVirtualDesktop.SelectedItem as DesktopInfo;
                    currentProfile.VirtualDesktopId = selectedDesktop?.Id;
                    currentProfile.MarkDirty();
                }
            };

            chkRenameDesktop.CheckedChanged += (_, __) =>
            {
                if (!isLoadingProfile && currentProfile != null)
                {
                    currentProfile.RenameVirtualDesktop = chkRenameDesktop.Checked;
                    currentProfile.MarkDirty();
                }
            };

            // Initial population of virtual desktops
            RefreshVirtualDesktopsList();

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

                // Show enhanced save details dialog
                using var dialog = new SaveDetailsDialog(currentProfile, savedCount, notRunningCount);
                dialog.ShowDialog(this);
            };

            btnApply.Click += async (_, __) =>
            {
                if (currentProfile == null) return;

                // ============================
                // VIRTUAL DESKTOP CONFIRMATION
                // ============================
                bool switchDesktop = false;

                if (!string.IsNullOrWhiteSpace(currentProfile.VirtualDesktopId))
                {
                    var desktop = VirtualDesktopHelper.FindDesktopById(currentProfile.VirtualDesktopId);
                    if (desktop != null)
                    {
                        var desktopInfo = VirtualDesktopHelper.GetAllDesktops()
                            .FirstOrDefault(d => d.Id == currentProfile.VirtualDesktopId);
                        string desktopName = desktopInfo?.Name ?? "selected desktop";

                        DialogResult result = MessageBox.Show(
                            this,
                            $"Apply this profile to {desktopName}, or apply to current desktop?\n\n" +
                            $"Yes = Switch to {desktopName} then apply\n" +
                            $"No = Apply to current desktop\n" +
                            $"Cancel = Cancel operation",
                            "Virtual Desktop Confirmation",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.Cancel)
                            return;

                        switchDesktop = (result == DialogResult.Yes);
                    }
                    else
                    {
                        // Desktop no longer exists
                        MessageBox.Show(this,
                            "The virtual desktop associated with this profile no longer exists.\n" +
                            "The profile will be applied to the current desktop.",
                            "Desktop Not Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        currentProfile.VirtualDesktopId = null;
                        currentProfile.MarkDirty();
                    }
                }

                btnApply.Enabled = false;
                UpdateStatusBar("Applying profile...");

                try
                {
                    // Switch desktop if requested
                    if (switchDesktop && !string.IsNullOrWhiteSpace(currentProfile.VirtualDesktopId))
                    {
                        UpdateStatusBar("Switching virtual desktop...");
                        bool switched = VirtualDesktopHelper.SwitchToDesktop(currentProfile.VirtualDesktopId);

                        if (!switched)
                        {
                            MessageBox.Show(this,
                                "Failed to switch to the selected virtual desktop.\n" +
                                "The profile will be applied to the current desktop.",
                                "Desktop Switch Failed",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        // Wait for desktop switch animation
                        await Task.Delay(800);
                    }

                    // Minimize all user windows BEFORE applying profile apps
                    WindowHelper.MinimizeAllUserWindows();
                    await profileApplier.ApplyProfileAsync(currentProfile);

                    // Update online status
                    isProfileApplied = true;
                    lblOnlineStatus.Text = "● ONLINE";
                    lblOnlineStatus.ForeColor = Theme.StatusOnline;
                    lblOnlineStatus.Visible = true;

                    UpdateStatusBar($"Profile '{currentProfile.Name}' applied successfully!");

                    // Handle minimize behavior after applying profile
                    await HandleMinimizeAfterApply();

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
                RestoreStartupWallpaper();

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
            btnMoveProfileUp.Enabled = lstProfiles.SelectedIndex > 0;
            btnMoveProfileDown.Enabled = lstProfiles.SelectedIndex < lstProfiles.Items.Count - 1;
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

            // Prevent volume changes while loading profile
            isLoadingProfile = true;
            try
            {
                volumeSlider.Value = profileVolume;
                volumeBox.Value = profileVolume;
            }
            finally
            {
                isLoadingProfile = false;
            }

            // -------------------------------
            // Load virtual desktop settings
            // -------------------------------
            isLoadingProfile = true;
            try
            {
                RefreshVirtualDesktopsList();

                if (currentProfile.VirtualDesktopId != null)
                {
                    // Find and select saved desktop
                    bool found = false;
                    for (int i = 0; i < cboVirtualDesktop.Items.Count; i++)
                    {
                        if (cboVirtualDesktop.Items[i] is DesktopInfo info &&
                            info.Id == currentProfile.VirtualDesktopId)
                        {
                            cboVirtualDesktop.SelectedIndex = i;
                            found = true;
                            break;
                        }
                    }

                    // If not found, reset to "None"
                    if (!found)
                    {
                        cboVirtualDesktop.SelectedIndex = 0;
                        currentProfile.VirtualDesktopId = null;
                        currentProfile.MarkDirty();
                        Logger.LogWarning($"Saved virtual desktop {currentProfile.VirtualDesktopId} no longer exists");
                    }
                }
                else
                {
                    cboVirtualDesktop.SelectedIndex = 0;
                }

                chkRenameDesktop.Checked = currentProfile.RenameVirtualDesktop;
            }
            finally
            {
                isLoadingProfile = false;
            }
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
            cboVirtualDesktop.SelectedIndex = 0;
            chkRenameDesktop.Checked = false;
            SetAppsEditorEnabled(false);
            btnApply.Enabled = false;
            btnKill.Enabled = false;
            btnRename.Enabled = false;
            btnDelete.Enabled = false;
            btnMoveProfileUp.Enabled = false;
            btnMoveProfileDown.Enabled = false;
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
                lblVirtualDesktop.Visible = true;
                cboVirtualDesktop.Visible = true;
                chkRenameDesktop.Visible = true;
                lblProfileTitle.Visible = true;
                lblOnlineStatus.Visible = currentProfile != null;
                btnToggleSuggestions.Text = "SUGGESTIONS";
                btnSettings.Visible = true;  // Show Settings button

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
                lblVirtualDesktop.Visible = false;
                cboVirtualDesktop.Visible = false;
                chkRenameDesktop.Visible = false;
                lblProfileTitle.Visible = false;
                lblOnlineStatus.Visible = false;
                btnToggleSuggestions.Text = "BACK";
                btnSettings.Visible = false;  // Hide Settings button
            }
        }

        private void ToggleSettings(object? sender, EventArgs e)
        {
            if (settingsPanel.Visible)
            {
                // Hide settings, show main UI
                settingsPanel.Visible = false;
                appsEditor.Visible = true;
                wallpaperEditor.Visible = true;
                lblVolume.Visible = !SettingsManager.CurrentSettings.DisableSystemVolumeModifications;
                volumeSlider.Visible = !SettingsManager.CurrentSettings.DisableSystemVolumeModifications;
                volumeBox.Visible = !SettingsManager.CurrentSettings.DisableSystemVolumeModifications;
                lblVirtualDesktop.Visible = !SettingsManager.CurrentSettings.DisableVirtualDesktopSupport;
                cboVirtualDesktop.Visible = !SettingsManager.CurrentSettings.DisableVirtualDesktopSupport;
                chkRenameDesktop.Visible = !SettingsManager.CurrentSettings.DisableVirtualDesktopSupport;
                lblProfileTitle.Visible = true;
                lblOnlineStatus.Visible = currentProfile != null;
                btnSettings.Text = "SETTINGS";
                btnToggleSuggestions.Visible = true;  // Show Suggestions button

                // Re-apply settings that might have changed
                ApplySettingsToUI();

                // Restore online/offline status
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
                // Show settings, hide main UI
                settingsPanel.LoadSettings();
                settingsPanel.Visible = true;
                appsEditor.Visible = false;
                wallpaperEditor.Visible = false;
                lblVolume.Visible = false;
                volumeSlider.Visible = false;
                volumeBox.Visible = false;
                lblVirtualDesktop.Visible = false;
                cboVirtualDesktop.Visible = false;
                chkRenameDesktop.Visible = false;
                lblProfileTitle.Visible = false;
                lblOnlineStatus.Visible = false;
                btnSettings.Text = "BACK";
                btnToggleSuggestions.Visible = false;  // Hide Suggestions button
            }
        }

        private void ApplySettingsToUI()
        {
            var settings = SettingsManager.CurrentSettings;

            // Hide/show volume controls
            lblVolume.Visible = !settings.DisableSystemVolumeModifications;
            volumeSlider.Visible = !settings.DisableSystemVolumeModifications;
            volumeBox.Visible = !settings.DisableSystemVolumeModifications;

            // Hide/show wallpaper editor
            wallpaperEditor.Visible = !settings.DisableWallpaperModifications;

            // Hide/show virtual desktop controls
            lblVirtualDesktop.Visible = !settings.DisableVirtualDesktopSupport;
            cboVirtualDesktop.Visible = !settings.DisableVirtualDesktopSupport;
            chkRenameDesktop.Visible = !settings.DisableVirtualDesktopSupport;
        }

        private void RefreshVirtualDesktopsList()
        {
            try
            {
                Logger.LogInfo("RefreshVirtualDesktopsList: Starting...");
                cboVirtualDesktop.Items.Clear();

                // Add "None" option at index 0
                var noneOption = new DesktopInfo { Id = null, Name = "(None - No Desktop Association)" };
                cboVirtualDesktop.Items.Add(noneOption);
                Logger.LogInfo("RefreshVirtualDesktopsList: Added 'None' option");

                if (!VirtualDesktopHelper.IsSupported())
                {
                    cboVirtualDesktop.SelectedIndex = 0;
                    cboVirtualDesktop.Enabled = false;
                    chkRenameDesktop.Enabled = false;
                    Logger.LogWarning("Virtual Desktop API not supported on this Windows version");
                    MessageBox.Show(this,
                        "Virtual Desktop features require Windows 10 build 19041 (20H1) or later.\n" +
                        "The Virtual Desktop dropdown will be disabled.",
                        "Virtual Desktop Not Supported",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var desktops = VirtualDesktopHelper.GetAllDesktops();
                Logger.LogInfo($"RefreshVirtualDesktopsList: Found {desktops.Count} virtual desktops");

                foreach (var desktop in desktops)
                {
                    cboVirtualDesktop.Items.Add(desktop);
                    Logger.LogInfo($"RefreshVirtualDesktopsList: Added desktop '{desktop.Name}' (ID: {desktop.Id})");
                }

                // Default: First actual desktop (index 1) or "None"
                cboVirtualDesktop.SelectedIndex = desktops.Count > 0 ? 1 : 0;
                cboVirtualDesktop.Enabled = true;
                chkRenameDesktop.Enabled = true;
                Logger.LogInfo($"RefreshVirtualDesktopsList: Completed. Total items: {cboVirtualDesktop.Items.Count}");
            }
            catch (Exception ex)
            {
                Logger.LogError("RefreshVirtualDesktopsList: Exception occurred", ex);
                MessageBox.Show(this,
                    $"Error loading virtual desktops: {ex.Message}\n\nThe Virtual Desktop dropdown will be disabled.",
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                cboVirtualDesktop.Enabled = false;
                chkRenameDesktop.Enabled = false;
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
            RestoreStartupWallpaper();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            // Titlebar minimize now just minimizes to taskbar (no auto-tray)
            // Use the "MINIMIZE TO TRAY" button for tray minimization
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
                    RestoreStartupWallpaper();
                    trayIcon.ShowBalloonTip(2000, "NoBS", $"Killed all apps for profile '{currentProfile.Name}'", ToolTipIcon.Info);
                }
            };
            menu.Items.Add(killAllItem);

            // Kill Profile & Exit
            var killAndExitItem = new ToolStripMenuItem("Kill Profile & Exit NoBS");
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
                        RestoreStartupWallpaper();
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

            // Move Up
            var moveUpItem = new ToolStripMenuItem("Move Up");
            moveUpItem.Enabled = lstProfiles.SelectedIndex > 0;
            moveUpItem.Click += (s, e) => MoveProfileUp(null, EventArgs.Empty);
            menu.Items.Add(moveUpItem);

            // Move Down
            var moveDownItem = new ToolStripMenuItem("Move Down");
            moveDownItem.Enabled = lstProfiles.SelectedIndex < lstProfiles.Items.Count - 1;
            moveDownItem.Click += (s, e) => MoveProfileDown(null, EventArgs.Empty);
            menu.Items.Add(moveDownItem);

            menu.Items.Add(new ToolStripSeparator());

            // Kill All Apps
            var killAllItem = new ToolStripMenuItem("Kill All Apps");
            killAllItem.Click += (s, e) =>
            {
                appRunner.KillAppsByProfile(profile.Apps);
                MessageBox.Show(this, $"Killed all apps for profile '{profile.Name}'", "Apps Killed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RestoreStartupWallpaper();
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

        private void RestoreStartupWallpaper()
        {
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
        }


        private void DrawProfileItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstProfiles.Items.Count)
                return;

            var profile = lstProfiles.Items[e.Index] as WorkspaceProfile;
            if (profile == null) return;

            // Background
            Color bgColor = (e.State & DrawItemState.Selected) != 0
                ? Theme.MidnightDark
                : Theme.DarkPanel;
            using (var bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            // Left crimson accent bar
            using (var accentBrush = new SolidBrush(Theme.MidnightBright))
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
            string name = Interaction.InputBox(
                "Enter profile name:",
                "Create Profile",
                ""
            );

            if (string.IsNullOrWhiteSpace(name))
                return;

            var profile = new WorkspaceProfile
            {
                Name = name
            };

            var result = MessageBox.Show(
                "Would you like to select from a list of currently opened windows to add to this profile?\n\n" +
                "Yes = Choose windows to include\nNo = Start fresh",
                "Create Profile",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    var currentWindows = ProfileSnapshotHelper.GetCurrentWindows();

                    if (currentWindows.Count == 0)
                    {
                        MessageBox.Show(
                            this,
                            "No eligible windows found on the desktop.",
                            "No Windows Detected",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        var selectedWindows = ProfileSnapshotHelper.ShowWindowSelectionDialog(currentWindows);

                        profile.Apps.AddRange(selectedWindows);
                        profile.CreatedFromSnapshot = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this,
                        $"Failed to capture/select windows:\n\n{ex.Message}",
                        "Window Selection Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }

            // Save the profile and reload UI
            ProfileManager.SaveProfile(profile);
            LoadProfiles();

            // Auto-select the newly created profile
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

        private void MoveProfileUp(object? sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is not WorkspaceProfile selectedProfile)
                return;

            var profiles = lstProfiles.Items.Cast<WorkspaceProfile>().ToList();
            int index = profiles.IndexOf(selectedProfile);

            if (index <= 0) return;

            profiles.RemoveAt(index);
            profiles.Insert(index - 1, selectedProfile);

            ProfileManager.ReorderProfiles(profiles);

            // Save the profile name to reselect after reload
            string selectedProfileName = selectedProfile.Name;

            LoadProfiles();

            // Reselect by name (since LoadProfiles creates new objects)
            lstProfiles.SelectedItem = lstProfiles.Items
                .Cast<WorkspaceProfile>()
                .FirstOrDefault(p => p.Name == selectedProfileName);
        }

        private void MoveProfileDown(object? sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is not WorkspaceProfile selectedProfile)
                return;

            var profiles = lstProfiles.Items.Cast<WorkspaceProfile>().ToList();
            int index = profiles.IndexOf(selectedProfile);

            if (index < 0 || index >= profiles.Count - 1) return;

            profiles.RemoveAt(index);
            profiles.Insert(index + 1, selectedProfile);

            ProfileManager.ReorderProfiles(profiles);

            // Save the profile name to reselect after reload
            string selectedProfileName = selectedProfile.Name;

            LoadProfiles();

            // Reselect by name (since LoadProfiles creates new objects)
            lstProfiles.SelectedItem = lstProfiles.Items
                .Cast<WorkspaceProfile>()
                .FirstOrDefault(p => p.Name == selectedProfileName);
        }

        private async Task HandleMinimizeAfterApply()
        {
            var settings = SettingsManager.CurrentSettings;

            // If set to "Ask Every Time" or first time, show dialog
            if (settings.MinimizeAfterApply == MinimizeAfterApplyBehavior.AskEveryTime ||
                !settings.HasShownMinimizePreferenceDialog)
            {
                var result = MessageBox.Show(
                    this,
                    "Would you like to minimize NoBS after applying profiles?\n\n" +
                    "Yes = Minimize to Tray (hidden)\n" +
                    "No = Minimize to Taskbar\n" +
                    "Cancel = Stay open\n\n" +
                    "You can change this in Settings later.",
                    "Minimize Preference",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question
                );

                // Save preference if this is the first time
                if (!settings.HasShownMinimizePreferenceDialog)
                {
                    settings.HasShownMinimizePreferenceDialog = true;

                    if (result == DialogResult.Yes)
                        settings.MinimizeAfterApply = MinimizeAfterApplyBehavior.MinimizeToTray;
                    else if (result == DialogResult.No)
                        settings.MinimizeAfterApply = MinimizeAfterApplyBehavior.MinimizeToTaskbar;
                    else
                        settings.MinimizeAfterApply = MinimizeAfterApplyBehavior.DoNotMinimize;

                    SettingsManager.Save();
                }

                // Apply the choice
                await Task.Delay(500); // Brief delay before minimizing

                if (result == DialogResult.Yes)
                {
                    Hide();
                    trayIcon.Visible = true;
                    trayIcon.ShowBalloonTip(2000, "NoBS", "Minimized to tray", ToolTipIcon.Info);
                }
                else if (result == DialogResult.No)
                {
                    WindowState = FormWindowState.Minimized;
                }
            }
            else
            {
                // Apply saved preference
                await Task.Delay(500);

                switch (settings.MinimizeAfterApply)
                {
                    case MinimizeAfterApplyBehavior.MinimizeToTray:
                        Hide();
                        trayIcon.Visible = true;
                        trayIcon.ShowBalloonTip(2000, "NoBS", "Minimized to tray", ToolTipIcon.Info);
                        break;
                    case MinimizeAfterApplyBehavior.MinimizeToTaskbar:
                        WindowState = FormWindowState.Minimized;
                        break;
                    case MinimizeAfterApplyBehavior.DoNotMinimize:
                        // Do nothing
                        break;
                }
            }
        }
    }
}
