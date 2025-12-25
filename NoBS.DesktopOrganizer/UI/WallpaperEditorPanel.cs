using NoBS.Core.Profiles;
using NoBS.DesktopOrganizer.Helpers;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class WallpaperEditorPanel : Panel
    {
        private WorkspaceProfile? profile;
        private bool suppressEvents;

        // UI Controls
        private Label lblTitle;
        private Button btnBrowse;
        private Button btnClear;
        private PictureBox picThumbnail;
        private Label lblFilePath;
        private Label lblNoWallpaper;

        public WallpaperEditorPanel()
        {
            Height = 180;
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
        }

        private void BuildUI()
        {
            // Title Label
            lblTitle = new Label
            {
                Text = "▌WALLPAPER",
                Left = 15,
                Top = 10,
                Font = Theme.SectionFont,
                ForeColor = Theme.MidnightBright,
                AutoSize = true
            };
            Controls.Add(lblTitle);

            // Browse Button
            btnBrowse = new AnimatedButton
            {
                Text = "BROWSE",
                Left = 15,
                Top = 40,
                Width = 100,
                Height = 32
            };
            btnBrowse.Click += BrowseWallpaper;
            Controls.Add(btnBrowse);

            // Clear Button (remove wallpaper from profile)
            btnClear = new AnimatedButton
            {
                Text = "CLEAR",
                Left = 125,
                Top = 40,
                Width = 80,
                Height = 32,
                Enabled = false
            };
            btnClear.ForeColor = Theme.Danger;
            btnClear.Click += ClearWallpaper;
            Controls.Add(btnClear);

            // File Path Label
            lblFilePath = new Label
            {
                Left = 15,
                Top = 80,
                Width = 200,
                Height = 40,
                Text = "No wallpaper set",
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 8.5f),
                AutoEllipsis = true
            };
            Controls.Add(lblFilePath);

            // Thumbnail PictureBox (240x135 = 16:9 ratio)
            picThumbnail = new PictureBox
            {
                Left = 230,
                Top = 23,
                Width = 240,
                Height = 135,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Background,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            picThumbnail.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.BorderDark, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, picThumbnail.Width - 1, picThumbnail.Height - 1);
                }
            };
            Controls.Add(picThumbnail);

            // "No wallpaper" overlay label
            lblNoWallpaper = new Label
            {
                Text = "NO WALLPAPER",
                Left = 230,
                Top = 23,
                Width = 240,
                Height = 135,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.StatusFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Panel,
                Visible = true
            };
            Controls.Add(lblNoWallpaper);
            lblNoWallpaper.BringToFront();
        }

        // ============================
        // Profile Management
        // ============================

        public void LoadProfile(WorkspaceProfile workspaceProfile)
        {
            profile = workspaceProfile;
            RefreshUI();
        }

        public void Clear()
        {
            profile = null;
            ClearThumbnail();
            lblFilePath.Text = "No wallpaper set";
            btnClear.Enabled = false;
        }

        private void RefreshUI()
        {
            if (profile == null)
            {
                Clear();
                return;
            }

            suppressEvents = true;

            if (!string.IsNullOrWhiteSpace(profile.WallpaperPath))
            {
                lblFilePath.Text = profile.WallpaperPath;
                btnClear.Enabled = true;
                LoadThumbnail(profile.WallpaperPath);
            }
            else
            {
                lblFilePath.Text = "No wallpaper set";
                btnClear.Enabled = false;
                ClearThumbnail();
            }

            suppressEvents = false;
        }

        // ============================
        // Thumbnail Management
        // ============================

        private void LoadThumbnail(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    ClearThumbnail();
                    lblNoWallpaper.Text = "File not found";
                    lblNoWallpaper.Visible = true;
                    return;
                }

                // Dispose previous image if any
                if (picThumbnail.Image != null)
                {
                    var oldImage = picThumbnail.Image;
                    picThumbnail.Image = null;
                    oldImage.Dispose();
                }

                // Load new image from file
                using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                {
                    picThumbnail.Image = Image.FromStream(fs);
                }

                lblNoWallpaper.Visible = false;
            }
            catch (Exception ex)
            {
                ClearThumbnail();
                lblNoWallpaper.Text = "Failed to load preview";
                lblNoWallpaper.Visible = true;
                NoBS.DesktopOrganizer.Core.Helpers.Logger.LogWarning(
                    $"Failed to load wallpaper thumbnail: {ex.Message}");
            }
        }

        private void ClearThumbnail()
        {
            if (picThumbnail.Image != null)
            {
                var oldImage = picThumbnail.Image;
                picThumbnail.Image = null;
                oldImage.Dispose();
            }

            lblNoWallpaper.Text = "No wallpaper selected";
            lblNoWallpaper.Visible = true;
        }

        // ============================
        // Event Handlers
        // ============================

        private void BrowseWallpaper(object? sender, EventArgs e)
        {
            if (profile == null) return;

            using var dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Select Wallpaper Image"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            // Validate image file
            if (!WallpaperHelper.IsValidImageFile(dlg.FileName))
            {
                MessageBox.Show(
                    "Selected file is not a valid image.",
                    "Invalid File",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Update profile
            profile.WallpaperPath = dlg.FileName;
            profile.MarkDirty();

            // Refresh UI
            RefreshUI();
        }

        private void ClearWallpaper(object? sender, EventArgs e)
        {
            if (profile == null) return;

            var result = MessageBox.Show(
                "Remove wallpaper from this profile?\n\nThe wallpaper will not be changed when this profile is applied.",
                "Clear Wallpaper",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            // Clear wallpaper from profile
            profile.WallpaperPath = null;
            profile.MarkDirty();

            // Refresh UI
            RefreshUI();
        }

        // ============================
        // Cleanup
        // ============================

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearThumbnail();
            }
            base.Dispose(disposing);
        }
    }
}
