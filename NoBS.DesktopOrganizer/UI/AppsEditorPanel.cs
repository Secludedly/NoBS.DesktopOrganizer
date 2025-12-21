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
                e.Graphics.DrawRectangle(new Pen(Theme.Border), 0, 0, Width - 1, Height - 1);
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
                Text = "Apps",
                Left = 15,
                Top = 10,
                Font = new Font("Segoe UI Semibold", 10f),
                ForeColor = Theme.Text
            };
            Controls.Add(lblTitle);

            lstApps = new ListBox
            {
                Left = 15,
                Top = 40,
                Width = 240,
                Height = 130,
                BackColor = Theme.Panel,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.FixedSingle,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28
            };
            lstApps.DrawItem += DrawAppItem;
            lstApps.SelectedIndexChanged += AppSelected;

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
            Controls.Add(lstApps);

            btnAdd = CreateButton("+ Add", 270, 40);
            btnAdd.Click += AddApp;
            Controls.Add(btnAdd);

            btnRemove = CreateButton("Remove", 270, 80);
            btnRemove.Click += RemoveApp;
            Controls.Add(btnRemove);

            var lblDelay = new Label
            {
                Text = "Launch Delay",
                Left = 15,
                Top = lstApps.Bottom + 10,
                ForeColor = Theme.TextMuted,
                AutoSize = true
            };
            Controls.Add(lblDelay);

            numDelay = new NumericUpDown
            {
                Left = lblDelay.Right + 15,
                Top = lblDelay.Top - 4,
                Width = 60,
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
                Left = numDelay.Right + 12,
                Top = lblDelay.Top - 2,
                ForeColor = Theme.Text,
                BackColor = Theme.Background,
                AutoSize = true,
                Enabled = false
            };
            chkKill.CheckedChanged += KillFlagChanged;
            Controls.Add(chkKill);
        }

        private Button CreateButton(string text, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Left = x,
                Top = y,
                Width = 90,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Panel,
                ForeColor = Theme.Text,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.FlatAppearance.BorderSize = 1;
            btn.MouseEnter += (_, __) => btn.FlatAppearance.BorderColor = Theme.Accent;
            btn.MouseLeave += (_, __) => btn.FlatAppearance.BorderColor = Theme.Border;
            return btn;
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

            e.DrawBackground();

            Color statusColor = app.Status switch
            {
                NoBS.Core.Profiles.AppRunStatus.Running => Color.LimeGreen,
                NoBS.Core.Profiles.AppRunStatus.Launching => Color.Gold,
                NoBS.Core.Profiles.AppRunStatus.Failed => Color.OrangeRed,
                _ => Color.DarkRed
            };

            using var brush = new SolidBrush(statusColor);
            e.Graphics.FillEllipse(brush, e.Bounds.Left + 6, e.Bounds.Top + 6, 12, 12);

            TextRenderer.DrawText(
                e.Graphics,
                app.Name,
                Font,
                new Rectangle(e.Bounds.Left + 26, e.Bounds.Top, e.Bounds.Width - 26, e.Bounds.Height),
                Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left
            );

            e.DrawFocusRectangle();
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
