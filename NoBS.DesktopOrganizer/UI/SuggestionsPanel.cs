using System;
using System.Drawing;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class SuggestionsPanel : Panel
    {
        private Panel contentPanel;

        public SuggestionsPanel()
        {
            BackColor = Theme.Background;
            AutoScroll = false;

            Paint += (_, e) =>
            {
                // Draw border
                using (var pen = new Pen(Theme.BorderCrimson, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
                using (var shadowPen = new Pen(Theme.ShadowDark, 1))
                {
                    e.Graphics.DrawRectangle(shadowPen, 1, 1, Width - 3, Height - 3);
                }
            };

            HandleCreated += (s, e) => BuildUI();
        }

        private void BuildUI()
        {
            var lblTitle = new Label
            {
                Text = "▌DEVELOPER SUGGESTIONS",
                Left = 15,
                Top = 10,
                Font = Theme.SectionFont,
                ForeColor = Theme.CrimsonBright,
                AutoSize = true
            };
            Controls.Add(lblTitle);

            contentPanel = new Panel
            {
                Left = 15,
                Top = 40,
                Width = Width - 35,
                Height = Height - 50,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            Controls.Add(contentPanel);

            int yPos = 0;
            var skyBlue = Color.FromArgb(135, 206, 250);

            // 🔧 System & Hardware
            yPos = AddCategoryHeader("🔧 SYSTEM", yPos);
            yPos = AddProgramLink("FanControl", "https://getfancontrol.com/", "Advanced control of CPU/GPU fan curves", yPos, skyBlue);
            yPos = AddProgramLink("MSI Afterburner", "https://www.msi.com/Landing/afterburner/graphics-cards", "GPU overclocking, monitoring, and fan tuning", yPos, skyBlue);
            yPos = AddProgramLink("Windows HDR Calibration", "https://www.microsoft.com/store/apps/9N7F2SM5D1LR", "Tune HDR brightness and color accuracy", yPos, skyBlue);

            yPos += 10; // Extra spacing between categories

            // 🎨 UI & Quality-of-Life
            yPos = AddCategoryHeader("🎨 UI", yPos);
            yPos = AddProgramLink("MicaForEveryone", "https://micaforeveryone.com/", "Apply Windows 11 Mica effects system-wide", yPos, skyBlue);
            yPos = AddProgramLink("EarTrumpet", "https://eartrumpet.app/", "Per-app volume control from the taskbar", yPos, skyBlue);
            yPos = AddProgramLink("Windows PowerToys", "https://github.com/microsoft/PowerToys/releases", "Power-user tools (FancyZones, Run, etc.)", yPos, skyBlue);

            yPos += 10;

            // 📸 Sharing & Transfers
            yPos = AddCategoryHeader("📸 SHARING", yPos);
            yPos = AddProgramLink("ShareX", "https://getsharex.com/", "Screenshots, screen recording, and file sharing", yPos, skyBlue);
            yPos = AddProgramLink("LocalSend", "https://localsend.org/", "Fast local network file transfers (cross-platform)", yPos, skyBlue);

            yPos += 10;

            // 🔍 Files, Search & Cleanup
            yPos = AddCategoryHeader("🔍 TOOLS", yPos);
            yPos = AddProgramLink("Everything", "https://www.voidtools.com/downloads/", "Instant file search using NTFS indexing", yPos, skyBlue);
            yPos = AddProgramLink("Revo Uninstaller", "https://www.revouninstaller.com/revo-uninstaller-free-download/", "Deep app uninstall + leftover removal", yPos, skyBlue);
            yPos = AddProgramLink("Glary Utilities", "https://www.glarysoft.com/", "System cleanup and maintenance tools", yPos, skyBlue);
            yPos = AddProgramLink("7-Zip", "https://sourceforge.net/projects/sevenzip/files/7-Zip/", "High-compression archive manager", yPos, skyBlue);

            yPos += 10;

            // 🌐 Internet & Downloads
            yPos = AddCategoryHeader("🌐 INTERNET", yPos);
            yPos = AddProgramLink("qBittorrent", "https://www.qbittorrent.org/", "Lightweight, ad-free torrent client", yPos, skyBlue);

            yPos += 10;

            // 📝 Media & Productivity
            yPos = AddCategoryHeader("📝 PRODUCTIVITY", yPos);
            yPos = AddProgramLink("Notepad++", "https://notepad-plus-plus.org/downloads/", "Lightweight code and text editor", yPos, skyBlue);
            yPos = AddProgramLink("VidCoder", "https://vidcoder.net/Documentation/Installation.html", "Simple video transcoding (HandBrake-based)", yPos, skyBlue);
            yPos = AddProgramLink("K-Lite Codec Pack", "https://www.codecguide.com/download_kl.htm", "Comprehensive media codec bundle", yPos, skyBlue);
            yPos = AddProgramLink("VLC Player", "https://www.videolan.org/vlc/", "Universal media player (plays damn near anything)", yPos, skyBlue);
        }

        private int AddCategoryHeader(string text, int yPos)
        {
            var lbl = new Label
            {
                Text = text,
                Left = 5,
                Top = yPos,
                Width = contentPanel.Width - 40,
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = Theme.CrimsonBright,
                AutoSize = false,
                Height = 20
            };
            contentPanel.Controls.Add(lbl);
            return yPos + 25;
        }

        private int AddProgramLink(string name, string url, string description, int yPos, Color linkColor)
        {
            var link = new LinkLabel
            {
                Text = name,
                Left = 20,
                Top = yPos,
                AutoSize = true,
                LinkColor = linkColor,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = linkColor,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            link.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            contentPanel.Controls.Add(link);

            var lblDesc = new Label
            {
                Text = $"— {description}",
                Left = 20,
                Top = yPos + 20,
                Width = contentPanel.Width - 40,
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                Height = 32
            };
            contentPanel.Controls.Add(lblDesc);

            return yPos + 55;
        }
    }
}
