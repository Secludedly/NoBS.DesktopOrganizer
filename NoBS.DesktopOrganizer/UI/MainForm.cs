using NoBS.Core.Profiles;
using NoBS.DesktopOrganizer.Core;
using NoBS.DesktopOrganizer.Core.Helpers;
using NoBS.DesktopOrganizer.Helpers;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class MainForm : Form
    {
        private ListBox lstProfiles;
        private Button btnCreateProfile;
        private Button btnRename;
        private Button btnDelete;

        private Button btnSave;
        private Button btnApply;
        private Button btnKill;

        private Panel rightPanel;
        private AppsEditorPanel appsEditor;
        private WallpaperEditorPanel wallpaperEditor;
        private Label lblProfileTitle;
        private Label lblAppsOverlay;
        private Label lblStatusBar;

        private readonly AppRunner appRunner = new();
        private readonly ProfileApplier profileApplier;
        private string? startupWallpaper;

        private WorkspaceProfile? currentProfile;

        public MainForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;

            if (Theme.Background == null)
                throw new InvalidOperationException("Theme must be initialized before MainForm");

            // Initialize logger
            NoBS.DesktopOrganizer.Core.Helpers.Logger.ClearLog();
            NoBS.DesktopOrganizer.Core.Helpers.Logger.LogInfo("=== NoBS Desktop Organizer Started ===");

            // Capture current wallpaper for restoration on exit
            startupWallpaper = WallpaperHelper.GetCurrentWallpaper();
            if (!string.IsNullOrEmpty(startupWallpaper))
            {
                NoBS.DesktopOrganizer.Core.Helpers.Logger.LogInfo($"Captured startup wallpaper: {startupWallpaper}");
            }

            profileApplier = new ProfileApplier(appRunner);

            InitializeComponent();
            LoadProfiles();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "NoBS - Desktop Organizer";
            ClientSize = new Size(900, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            KeyPreview = true;

            // Add keyboard shortcuts
            KeyDown += MainForm_KeyDown;

            // ============================
            // PROFILES LIST
            // ============================
            lstProfiles = new ListBox
            {
                Left = 10,
                Top = 10,
                Width = 200,
                Height = ClientSize.Height - 180,
                BackColor = Theme.Panel,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.FixedSingle,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 32
            };
            lstProfiles.DrawItem += DrawProfileItem;
            lstProfiles.SelectedIndexChanged += ProfileSelected;
            Controls.Add(lstProfiles);

            // ============================
            // PROFILE BUTTONS
            // ============================
            int buttonsTop = lstProfiles.Bottom + 10;

            btnCreateProfile = CreateButton("+ Create", 10, buttonsTop);
            btnCreateProfile.Width = 200;
            btnCreateProfile.Click += CreateProfile;

            btnDelete = CreateButton("Delete", 10, buttonsTop + 38, Color.FromArgb(140, 60, 60));
            btnDelete.Width = 95;
            btnDelete.Enabled = false;
            btnDelete.Click += DeleteProfile;

            btnRename = CreateButton("Rename", 115, buttonsTop + 38);
            btnRename.Width = 95;
            btnRename.Enabled = false;
            btnRename.Click += RenameProfile;

            Controls.AddRange(new Control[] { btnCreateProfile, btnDelete, btnRename });

            // ============================
            // RIGHT PANEL
            // ============================
            rightPanel = new Panel
            {
                Left = 220,
                Top = 10,
                Width = ClientSize.Width - 230,
                Height = ClientSize.Height - 80,
                BackColor = Theme.Panel,
                AutoScroll = true
            };
            Controls.Add(rightPanel);

            lblProfileTitle = new Label
            {
                Text = "No profile selected",
                Left = 20,
                Top = 20,
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = Theme.Text,
                AutoSize = true
            };
            rightPanel.Controls.Add(lblProfileTitle);

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
                Top = 290,  // Below AppsEditorPanel (60 + 220 + 10)
                Width = rightPanel.Width - 60,
                Height = 180
            };
            rightPanel.Controls.Add(wallpaperEditor);

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
            // BOTTOM BAR
            // ============================
            int bottomY = ClientSize.Height - 63;

            btnSave = CreateButton("Save", 230, bottomY);
            btnApply = CreateButton("Apply", 320, bottomY);
            btnKill = CreateButton("Kill Apps", 410, bottomY, Color.FromArgb(140, 60, 60));

            btnApply.Enabled = false;
            btnKill.Enabled = false;

            Controls.AddRange(new Control[] { btnSave, btnApply, btnKill });

            // ============================
            // STATUS BAR
            // ============================
            lblStatusBar = new Label
            {
                Left = 230,
                Top = ClientSize.Height - 26,
                Width = ClientSize.Width - 240,
                Height = 18,
                Text = "Ready | Shortcuts: Ctrl+S (Save), Ctrl+Enter (Apply), F5 (Refresh)",
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 8f),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(lblStatusBar);

            // ============================
            // BUTTON LOGIC
            // ============================

            btnSave.Click += (_, __) =>
            {
                if (currentProfile == null)
                    return;

                int savedCount = 0;
                int notRunningCount = 0;

                // First, refresh statuses to get latest positions from global registry
                appRunner.RefreshAppStatuses(currentProfile.Apps);

                foreach (var app in currentProfile.Apps)
                {
                    // If app is running, its position is already updated by monitoring
                    if (app.ProcessId.HasValue && app.WindowHandle != IntPtr.Zero)
                    {
                        // Verify current position one more time
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
                        // Try to find window by executable path for non-monitored apps
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
            };

            ResumeLayout(false);
        }

        // ============================
        // HELPERS
        // ============================

        private Button CreateButton(string text, int x, int y, Color? bg = null)
        {
            var btn = new Button
            {
                Text = text,
                Left = x,
                Top = y,
                Width = 90,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg ?? Theme.Panel,
                ForeColor = Theme.Text
            };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private void LoadProfiles()
        {
            lstProfiles.Items.Clear();
            foreach (var p in ProfileManager.LoadAllProfiles())
                lstProfiles.Items.Add(p.Name);
        }

        private void ProfileSelected(object? sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem == null)
            {
                ClearProfile();
                return;
            }

            // Load the profile object from storage
            currentProfile = ProfileManager
                .LoadAllProfiles()
                .FirstOrDefault(p => p.Name == lstProfiles.SelectedItem.ToString());

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
            lblProfileTitle.Text = currentProfile.Name;
            appsEditor.LoadProfile(currentProfile);
            wallpaperEditor.LoadProfile(currentProfile);

            // Refresh statuses immediately when switching profiles
            appsEditor.RefreshStatuses();

            // -------------------------------
            // Enable buttons depending on app availability
            // -------------------------------
            btnRename.Enabled = true;
            btnDelete.Enabled = true;
            btnApply.Enabled = currentProfile.Apps.Any();
            btnKill.Enabled = currentProfile.Apps.Any();

            SetAppsEditorEnabled(true);

            // Update status bar with app counts
            int runningApps = currentProfile.Apps.Count(a => a.Status == NoBS.Core.Profiles.AppRunStatus.Running);
            int totalApps = currentProfile.Apps.Count;
            UpdateStatusBar($"Profile: {currentProfile.Name} | Apps: {runningApps}/{totalApps} running | Shortcuts: Ctrl+S (Save), Ctrl+Enter (Apply), F5 (Refresh)");
        }

        private void ClearProfile()
        {
            currentProfile = null;
            lblProfileTitle.Text = "No profile selected";
            appsEditor.Clear();
            wallpaperEditor.Clear();

            btnRename.Enabled = false;
            btnDelete.Enabled = false;
            btnApply.Enabled = false;
            btnKill.Enabled = false;

            SetAppsEditorEnabled(false);
        }

        private void SetAppsEditorEnabled(bool enabled)
        {
            appsEditor.Enabled = enabled;
            wallpaperEditor.Enabled = enabled;
            lblAppsOverlay.Visible = !enabled;
            if (!enabled) lblAppsOverlay.BringToFront();
        }

        private void DrawProfileItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool selected = (e.State & DrawItemState.Selected) != 0;
            var bg = selected ? Theme.AccentSoft : Theme.Panel;
            var fg = Theme.Text;

            e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);
            TextRenderer.DrawText(
                e.Graphics,
                lstProfiles.Items[e.Index].ToString(),
                Font,
                e.Bounds,
                fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left
            );
        }

        private void CreateProfile(object? sender, EventArgs e)
        {
            using var dlg = new CreateProfileForm();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            ProfileManager.SaveProfile(dlg.CreatedProfile);
            LoadProfiles();
        }

        private void DeleteProfile(object? sender, EventArgs e)
        {
            if (currentProfile == null) return;

            var result = MessageBox.Show(
                this,
                $"Are you sure you want to delete profile '{currentProfile.Name}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            NoBS.DesktopOrganizer.Core.Helpers.Logger.LogInfo($"Deleting profile: {currentProfile.Name}");
            ProfileManager.DeleteProfile(currentProfile.Name);
            ClearProfile();
            LoadProfiles();
        }

        private void RenameProfile(object? sender, EventArgs e)
        {
            if (currentProfile == null) return;

            using var dlg = new InputDialog("Rename Profile", "New name:", currentProfile.Name);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            if (!ProfileManager.RenameProfile(currentProfile.Name, dlg.Value))
            {
                MessageBox.Show(this, "Name already exists.", "Rename Failed");
                return;
            }

            currentProfile.Name = dlg.Value;
            LoadProfiles();
            lstProfiles.SelectedItem = dlg.Value;
        }

        private bool ConfirmDiscardChanges()
        {
            if (currentProfile == null)
                return true;

            if (!currentProfile.IsDirty)
                return true;

            var result = MessageBox.Show(
                this,
                $"Profile \"{currentProfile.Name}\" has unsaved changes.\n\nDiscard them?",
                "Unsaved Changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return result == DialogResult.Yes;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!ConfirmDiscardChanges())
            {
                e.Cancel = true;
                return;
            }

            // Restore original wallpaper on exit
            if (!string.IsNullOrEmpty(startupWallpaper))
            {
                NoBS.DesktopOrganizer.Core.Helpers.Logger.LogInfo("Restoring startup wallpaper on exit");
                WallpaperHelper.SetWallpaper(startupWallpaper);
            }

            NoBS.DesktopOrganizer.Core.Helpers.Logger.LogInfo("=== NoBS Desktop Organizer Closed ===");
            base.OnFormClosing(e);
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl+S to Save
            if (e.Control && e.KeyCode == Keys.S && currentProfile != null)
            {
                e.Handled = true;
                btnSave.PerformClick();
            }
            // Ctrl+Enter to Apply
            else if (e.Control && e.KeyCode == Keys.Return && btnApply.Enabled)
            {
                e.Handled = true;
                btnApply.PerformClick();
            }
            // F5 to refresh statuses
            else if (e.KeyCode == Keys.F5 && currentProfile != null)
            {
                e.Handled = true;
                UpdateStatusBar("Refreshing app statuses...");
                appRunner.RefreshAppStatuses(currentProfile.Apps);
                appsEditor.RefreshStatuses();
                UpdateStatusBar("Ready");
            }
        }

        private void UpdateStatusBar(string message)
        {
            if (lblStatusBar != null && !lblStatusBar.IsDisposed)
            {
                if (lblStatusBar.InvokeRequired)
                {
                    lblStatusBar.Invoke(() => lblStatusBar.Text = message);
                }
                else
                {
                    lblStatusBar.Text = message;
                }
            }
        }
    }
}
