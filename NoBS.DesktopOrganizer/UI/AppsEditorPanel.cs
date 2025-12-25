using NoBS.Core;
using NoBS.Core.Profiles;
using NoBS.DesktopOrganizer.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class AppsEditorPanel : Panel
    {
        private readonly ToolTip appTip = new();
        private readonly System.Windows.Forms.Timer statusTimer = new System.Windows.Forms.Timer();
        private readonly AppRunner runner;

        private ListBox lstApps;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnMoveUp;
        private Button btnMoveDown;
        private NumericUpDown numDelay;
        private CheckBox chkKill;

        private WorkspaceProfile? profile;
        private bool suppressEvents;

        public AppsEditorPanel(AppRunner runner)
        {
            this.runner = runner ?? throw new ArgumentNullException(nameof(runner));

            Height = 240;
            BackColor = Theme.Background;

            Paint += (_, e) =>
            {
                // Draw crimson border
                using (var pen = new Pen(Theme.BorderMidnight, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
                // Draw inner shadow
                using (var shadowPen = new Pen(Theme.ShadowDark, 1))
                {
                    e.Graphics.DrawRectangle(shadowPen, 1, 1, Width - 3, Height - 3);
                }
            };

            BuildUI();

            // Timer setup
            statusTimer.Interval = 2500; // 2.5s
            statusTimer.Tick += (_, __) =>
            {
                if (profile == null) return;

                RefreshStatuses();
            };
            statusTimer.Start();
        }

        private void BuildUI()
        {
            var lblTitle = new Label
            {
                Text = "▌APPLICATIONS",
                Left = 15,
                Top = 10,
                Font = Theme.SectionFont,
                ForeColor = Theme.MidnightBright,
                AutoSize = true
            };
            Controls.Add(lblTitle);

            lstApps = new ListBox
            {
                Left = 15,
                Top = 40,
                Width = 240,
                Height = 150,
                BackColor = Theme.Background,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 32,
                Font = Theme.TextFont
            };
            lstApps.DrawItem += DrawAppItem;
            lstApps.SelectedIndexChanged += AppSelected;
            lstApps.MouseDown += LstApps_MouseDown;

            appTip.ShowAlways = true;
            lstApps.MouseMove += (_, e) =>
            {
                int index = lstApps.IndexFromPoint(e.Location);
                if (index < 0) return;

                var app = (WindowPosition)lstApps.Items[index];
                appTip.SetToolTip(lstApps,
                    $"Status: {app.Status}\n" +
                    $"PID: {app.ProcessId?.ToString() ?? "N/A"}\n" +
                    $"HWND: {(app.WindowHandle != IntPtr.Zero ? app.WindowHandle.ToString("X") : "N/A")}\n" +
                    $"Error: {app.LastError ?? "None"}");
            };
            // Add border around list
            var appsContainer = new Panel
            {
                Left = 13,
                Top = 38,
                Width = lstApps.Width + 4,
                Height = lstApps.Height + 4,
                BackColor = Theme.DarkPanel
            };
            appsContainer.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.BorderDark, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, appsContainer.Width - 1, appsContainer.Height - 1);
                }
            };
            Controls.Add(appsContainer);
            appsContainer.Controls.Add(lstApps);
            lstApps.Left = 2;
            lstApps.Top = 2;

            btnAdd = new AnimatedButton
            {
                Text = "+ ADD",
                Left = 270,
                Top = 40,
                Width = 90,
                Height = 32
            };
            btnAdd.Click += AddApp;
            Controls.Add(btnAdd);

            btnRemove = new AnimatedButton
            {
                Text = "REMOVE",
                Left = 270,
                Top = 78,
                Width = 90,
                Height = 32
            };
            btnRemove.ForeColor = Theme.Danger;
            btnRemove.Click += RemoveApp;
            Controls.Add(btnRemove);

            btnMoveUp = new AnimatedButton
            {
                Text = "↑ UP",
                Left = 370,
                Top = 40,
                Width = 90,
                Height = 32,
                Enabled = false
            };
            btnMoveUp.Click += MoveAppUp;
            Controls.Add(btnMoveUp);

            btnMoveDown = new AnimatedButton
            {
                Text = "↓ DOWN",
                Left = 370,
                Top = 78,
                Width = 90,
                Height = 32,
                Enabled = false
            };
            btnMoveDown.Click += MoveAppDown;
            Controls.Add(btnMoveDown);

            var lblDelay = new Label
            {
                Text = "Launch Delay",
                Left = 266,
                Top = btnRemove.Bottom + 12,
                ForeColor = Theme.TextMuted,
                AutoSize = true
            };
            Controls.Add(lblDelay);

            numDelay = new NumericUpDown
            {
                Left = 270,
                Top = lblDelay.Bottom + 4,
                Width = 90,
                Minimum = 0,
                Maximum = 300,
                BackColor = Theme.Panel,
                ForeColor = Theme.Text,
                Enabled = false
            };
            numDelay.ValueChanged += DelayChanged;
            Controls.Add(numDelay);

            chkKill = new CheckBox
            {
                Text = "Kill app on profile switch",
                Left = 270,
                Top = numDelay.Bottom + 8,
                Width = 200,
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                AutoSize = false,
                Enabled = false
            };
            chkKill.CheckedChanged += KillFlagChanged;
            Controls.Add(chkKill);
        }


        public void LoadProfile(WorkspaceProfile workspaceProfile)
        {
            profile = workspaceProfile;
            RefreshList();
            ClearSelection();
        }

        public void Clear()
        {
            profile = null;
            lstApps.Items.Clear();
            ClearSelection();
        }

        public void RefreshStatuses()
        {
            if (profile == null) return;

            // Refresh real app statuses
            runner.RefreshAppStatuses(profile.Apps);

            // Redraw the list so status circles and tooltips update
            lstApps.Refresh();
        }

        private void RefreshList()
        {
            lstApps.Items.Clear();
            if (profile == null) return;

            foreach (var app in profile.Apps)
                lstApps.Items.Add(app);

            lstApps.Invalidate();
        }

        // =========================
        // SELECTION HANDLING
        // =========================
        private void AppSelected(object? sender, EventArgs e)
        {
            if (lstApps.SelectedItem is not WindowPosition app)
            {
                ClearSelection();
                return;
            }

            suppressEvents = true;

            numDelay.Enabled = true;
            chkKill.Enabled = true;

            // Enable/disable move buttons based on position
            if (profile != null)
            {
                int index = profile.Apps.IndexOf(app);
                btnMoveUp.Enabled = index > 0;
                btnMoveDown.Enabled = index < profile.Apps.Count - 1;
            }

            numDelay.Value = Math.Clamp(app.LaunchDelaySeconds, (int)numDelay.Minimum, (int)numDelay.Maximum);
            chkKill.Checked = app.KillOnSwitch;

            suppressEvents = false;
        }

        private void ClearSelection()
        {
            suppressEvents = true;

            lstApps.ClearSelected();
            numDelay.Value = 0;
            numDelay.Enabled = false;

            chkKill.Checked = false;
            chkKill.Enabled = false;

            btnMoveUp.Enabled = false;
            btnMoveDown.Enabled = false;

            suppressEvents = false;
        }

        // =========================
        // DIRTY-TRACKED CHANGES
        // =========================
        private void DelayChanged(object? sender, EventArgs e)
        {
            if (suppressEvents || profile == null || lstApps.SelectedItem is not WindowPosition app)
                return;

            app.LaunchDelaySeconds = (int)numDelay.Value;
            profile.MarkDirty();
        }

        private void KillFlagChanged(object? sender, EventArgs e)
        {
            if (suppressEvents || profile == null || lstApps.SelectedItem is not WindowPosition app)
                return;

            app.KillOnSwitch = chkKill.Checked;
            profile.MarkDirty();
        }

        // =========================
        // RIGHT-CLICK CONTEXT MENU
        // =========================
        private void LstApps_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = lstApps.IndexFromPoint(e.Location);
                if (index >= 0 && index < lstApps.Items.Count)
                {
                    lstApps.SelectedIndex = index;
                    var app = lstApps.Items[index] as WindowPosition;
                    if (app != null)
                    {
                        var contextMenu = CreateAppContextMenu(app);
                        contextMenu.Show(lstApps, e.Location);
                    }
                }
            }
        }

        private ContextMenuStrip CreateAppContextMenu(WindowPosition app)
        {
            var menu = new ContextMenuStrip();

            // Kill Process
            var killItem = new ToolStripMenuItem("Kill Process");
            killItem.Enabled = app.ProcessId.HasValue;
            killItem.Click += (s, e) =>
            {
                if (app.ProcessId.HasValue)
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById(app.ProcessId.Value);
                        if (!process.HasExited)
                        {
                            process.Kill();
                            MessageBox.Show($"Killed process for '{app.Name}'", "Process Killed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            RefreshStatuses();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to kill process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            menu.Items.Add(killItem);

            // Re-open
            var reopenItem = new ToolStripMenuItem("Re-open");
            reopenItem.Click += async (s, e) =>
            {
                // Kill if running
                if (app.ProcessId.HasValue)
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById(app.ProcessId.Value);
                        if (!process.HasExited)
                        {
                            process.Kill();
                            await System.Threading.Tasks.Task.Delay(500);
                        }
                    }
                    catch { }
                }

                // Store original saved coordinates
                int targetX = app.X;
                int targetY = app.Y;
                int targetWidth = app.Width;
                int targetHeight = app.Height;

                // Check if saved coordinates are on an active monitor
                var allScreens = System.Windows.Forms.Screen.AllScreens;
                bool isOnActiveMonitor = allScreens.Any(screen => screen.Bounds.Contains(targetX, targetY));

                if (!isOnActiveMonitor && targetWidth > 0 && targetHeight > 0)
                {
                    // Coordinates are on disabled monitor, adjust to primary
                    var primaryMonitor = System.Windows.Forms.Screen.PrimaryScreen;
                    if (targetWidth <= primaryMonitor.Bounds.Width && targetHeight <= primaryMonitor.Bounds.Height)
                    {
                        targetX = primaryMonitor.Bounds.X + (primaryMonitor.Bounds.Width - targetWidth) / 2;
                        targetY = primaryMonitor.Bounds.Y + (primaryMonitor.Bounds.Height - targetHeight) / 2;
                    }
                    else
                    {
                        targetX = primaryMonitor.Bounds.X + 50;
                        targetY = primaryMonitor.Bounds.Y + 50;
                    }
                }

                // Reset process/window info
                app.ProcessId = null;
                app.WindowHandle = IntPtr.Zero;

                // Re-launch
                var apps = new List<WindowPosition> { app };
                await runner.LaunchAppsAsync(apps);

                // Wait for window to appear
                await System.Threading.Tasks.Task.Delay(2000);

                // Apply position if available
                if (targetWidth > 0 && targetHeight > 0 && app.ProcessId.HasValue)
                {
                    IntPtr hWnd = app.WindowHandle;
                    if (hWnd == IntPtr.Zero)
                    {
                        hWnd = NoBS.DesktopOrganizer.Core.Helpers.WindowPositionHelper.FindWindowByProcessId(app.ProcessId.Value);
                        if (hWnd != IntPtr.Zero)
                            app.WindowHandle = hWnd;
                    }

                    if (hWnd != IntPtr.Zero)
                    {
                        // Force window to target position (may be adjusted if monitor disabled)
                        NoBS.DesktopOrganizer.Core.Helpers.WindowPositionHelper.SetWindowPosition(hWnd, targetX, targetY, targetWidth, targetHeight);

                        // Wait and force again to ensure it sticks
                        await System.Threading.Tasks.Task.Delay(300);
                        NoBS.DesktopOrganizer.Core.Helpers.WindowPositionHelper.SetWindowPosition(hWnd, targetX, targetY, targetWidth, targetHeight);
                    }
                }

                RefreshStatuses();
            };
            menu.Items.Add(reopenItem);

            // Re-open Fresh
            var reopenFreshItem = new ToolStripMenuItem("Re-open Fresh");
            reopenFreshItem.Font = new Font(reopenFreshItem.Font, FontStyle.Bold);
            reopenFreshItem.Click += async (s, e) =>
            {
                // Kill if running
                if (app.ProcessId.HasValue)
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById(app.ProcessId.Value);
                        if (!process.HasExited)
                        {
                            process.Kill();
                            await System.Threading.Tasks.Task.Delay(500);
                        }
                    }
                    catch { }
                }

                // Clear saved position/size
                int savedX = app.X;
                int savedY = app.Y;
                int savedWidth = app.Width;
                int savedHeight = app.Height;

                app.X = 0;
                app.Y = 0;
                app.Width = 0;
                app.Height = 0;
                app.ProcessId = null;
                app.WindowHandle = IntPtr.Zero;

                // Re-launch fresh
                var apps = new List<WindowPosition> { app };
                await runner.LaunchAppsAsync(apps);

                // Wait for window to stabilize, then get new size
                await System.Threading.Tasks.Task.Delay(2000);

                if (app.ProcessId.HasValue)
                {
                    IntPtr hWnd = app.WindowHandle;
                    if (hWnd == IntPtr.Zero)
                    {
                        hWnd = NoBS.DesktopOrganizer.Core.Helpers.WindowPositionHelper.FindWindowByProcessId(app.ProcessId.Value);
                        if (hWnd != IntPtr.Zero)
                            app.WindowHandle = hWnd;
                    }

                    if (hWnd != IntPtr.Zero)
                    {
                        var rect = NoBS.DesktopOrganizer.Core.Helpers.WindowPositionHelper.GetWindowRectSafe(hWnd);
                        if (rect.HasValue)
                        {
                            app.X = rect.Value.X;
                            app.Y = rect.Value.Y;
                            app.Width = rect.Value.Width;
                            app.Height = rect.Value.Height;
                        }
                    }
                }

                profile?.MarkDirty();
                RefreshStatuses();
                MessageBox.Show($"Re-opened '{app.Name}' fresh. New position and size saved.", "Re-opened Fresh", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            menu.Items.Add(reopenFreshItem);

            menu.Items.Add(new ToolStripSeparator());

            // Move Up
            var moveUpItem = new ToolStripMenuItem("Move Up");
            if (profile != null)
            {
                int index = profile.Apps.IndexOf(app);
                moveUpItem.Enabled = index > 0;
            }
            moveUpItem.Click += (s, e) => MoveAppUp(null, EventArgs.Empty);
            menu.Items.Add(moveUpItem);

            // Move Down
            var moveDownItem = new ToolStripMenuItem("Move Down");
            if (profile != null)
            {
                int index = profile.Apps.IndexOf(app);
                moveDownItem.Enabled = index < profile.Apps.Count - 1;
            }
            moveDownItem.Click += (s, e) => MoveAppDown(null, EventArgs.Empty);
            menu.Items.Add(moveDownItem);

            return menu;
        }

        // =========================
        // APP MANAGEMENT
        // =========================
        private void AddApp(object? sender, EventArgs e)
        {
            if (profile == null) return;

            using var dlg = new OpenFileDialog
            {
                Filter = "Programs (*.exe)|*.exe",
                Title = "Select application"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            // Check for duplicate apps
            string normalizedPath = System.IO.Path.GetFullPath(dlg.FileName).ToLowerInvariant();
            bool isDuplicate = profile.Apps.Any(a =>
            {
                try
                {
                    return System.IO.Path.GetFullPath(a.ExecutablePath).ToLowerInvariant() == normalizedPath;
                }
                catch
                {
                    return a.ExecutablePath.ToLowerInvariant() == normalizedPath;
                }
            });

            if (isDuplicate)
            {
                MessageBox.Show("This application is already in the profile.", "Duplicate Application",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var entry = new WindowPosition
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName),
                ExecutablePath = dlg.FileName,
                LaunchDelaySeconds = 0,
                KillOnSwitch = true
            };

            profile.Apps.Add(entry);
            profile.MarkDirty();

            RefreshList();
            lstApps.SelectedItem = entry;
        }

        private void RemoveApp(object? sender, EventArgs e)
        {
            if (profile == null || lstApps.SelectedItem is not WindowPosition app) return;

            profile.Apps.Remove(app);
            profile.MarkDirty();

            RefreshList();
            ClearSelection();
        }

        private void MoveAppUp(object? sender, EventArgs e)
        {
            if (profile == null || lstApps.SelectedItem is not WindowPosition app) return;

            int currentIndex = profile.Apps.IndexOf(app);
            if (currentIndex <= 0) return; // Already at top

            // Swap with previous item
            profile.Apps.RemoveAt(currentIndex);
            profile.Apps.Insert(currentIndex - 1, app);
            profile.MarkDirty();

            RefreshList();
            lstApps.SelectedItem = app; // Maintain selection
        }

        private void MoveAppDown(object? sender, EventArgs e)
        {
            if (profile == null || lstApps.SelectedItem is not WindowPosition app) return;

            int currentIndex = profile.Apps.IndexOf(app);
            if (currentIndex >= profile.Apps.Count - 1) return; // Already at bottom

            // Swap with next item
            profile.Apps.RemoveAt(currentIndex);
            profile.Apps.Insert(currentIndex + 1, app);
            profile.MarkDirty();

            RefreshList();
            lstApps.SelectedItem = app; // Maintain selection
        }

        // =========================
        // OWNER DRAW + STATUS REFRESH
        // =========================
        private void DrawAppItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var app = (WindowPosition)lstApps.Items[e.Index];

            // 🔄 Refresh status dynamically for this list item
            if (app.ProcessId.HasValue)
            {
                try
                {
                    var proc = System.Diagnostics.Process.GetProcessById(app.ProcessId.Value);
                    if (proc.HasExited)
                    {
                        app.Status = NoBS.Core.Profiles.AppRunStatus.NotRunning;
                        app.ProcessId = null;
                        app.WindowHandle = IntPtr.Zero;
                    }
                    else
                    {
                        app.Status = NoBS.Core.Profiles.AppRunStatus.Running;
                        app.WindowHandle = proc.MainWindowHandle;
                    }
                }
                catch
                {
                    app.Status = NoBS.Core.Profiles.AppRunStatus.NotRunning;
                    app.ProcessId = null;
                    app.WindowHandle = IntPtr.Zero;
                }
            }
            else
            {
                if (app.Status != NoBS.Core.Profiles.AppRunStatus.Launching && app.Status != NoBS.Core.Profiles.AppRunStatus.Failed)
                    app.Status = NoBS.Core.Profiles.AppRunStatus.NotRunning;
            }

            // Background
            Color bgColor = (e.State & DrawItemState.Selected) != 0
                ? Theme.MidnightDark
                : Theme.Background;
            using (var bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            // Status color
            Color statusColor = app.Status switch
            {
                NoBS.Core.Profiles.AppRunStatus.Running => Theme.StatusOnline,
                NoBS.Core.Profiles.AppRunStatus.Launching => Color.FromArgb(200, 180, 40),
                NoBS.Core.Profiles.AppRunStatus.Failed => Theme.Danger,
                _ => Theme.StatusOffline
            };

            // Status circle
            using (var statusBrush = new SolidBrush(statusColor))
            {
                e.Graphics.FillEllipse(statusBrush, e.Bounds.Left + 8, e.Bounds.Top + 10, 12, 12);
            }

            // App name text
            Color textColor = (e.State & DrawItemState.Selected) != 0
                ? Theme.TextBright
                : Theme.Text;
            using (var textBrush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(app.Name, Font, textBrush, e.Bounds.Left + 28, e.Bounds.Top + 8);
            }

            // Bottom border
            using (var borderPen = new Pen(Theme.BorderDark, 1))
            {
                e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        // =========================
        // EXPORT (SAFE COPY)
        // =========================
        public List<WindowPosition> GetApps()
        {
            if (profile == null) return new();

            return profile.Apps
                .Select(a => new WindowPosition
                {
                    Name = a.Name,
                    ExecutablePath = a.ExecutablePath,
                    LaunchDelaySeconds = a.LaunchDelaySeconds,
                    KillOnSwitch = a.KillOnSwitch
                })
                .ToList();
        }
    }
}
