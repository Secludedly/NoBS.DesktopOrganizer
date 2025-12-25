using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class AnimatedButton : Button
    {
        private System.Windows.Forms.Timer glowTimer;
        private System.Windows.Forms.Timer shakeTimer;
        private int glowPhase = 0;
        private int shakeOffset = 0;
        private bool isShaking = false;
        private int originalX;
        private Color currentGlowColor;
        private string? storedText;

        public AnimatedButton()
        {
            FlatStyle = FlatStyle.Flat;
            BackColor = Theme.Panel;
            ForeColor = Color.White;
            Font = Theme.ButtonFont;
            Cursor = Cursors.Hand;

            FlatAppearance.BorderSize = 2;
            FlatAppearance.BorderColor = Theme.BorderMidnight;
            FlatAppearance.MouseOverBackColor = Theme.Panel;
            FlatAppearance.MouseDownBackColor = Theme.DarkPanel;

            // Glow animation timer
            glowTimer = new System.Windows.Forms.Timer { Interval = 50 };
            glowTimer.Tick += GlowTimer_Tick;
            glowTimer.Start();

            // Shake animation timer
            shakeTimer = new System.Windows.Forms.Timer { Interval = 30 };
            shakeTimer.Tick += ShakeTimer_Tick;

            MouseEnter += AnimatedButton_MouseEnter;
            MouseLeave += AnimatedButton_MouseLeave;

            // Custom paint for glow effect
            Paint += AnimatedButton_Paint;

            // Handle enabled state changes
            EnabledChanged += AnimatedButton_EnabledChanged;
        }

        private void AnimatedButton_EnabledChanged(object sender, EventArgs e)
        {
            if (Enabled)
            {
                // Enabled: restore text and styling
                if (storedText != null)
                {
                    Text = storedText;
                    storedText = null;
                }
                ForeColor = Color.White;
                Cursor = Cursors.Hand;
            }
            else
            {
                // Disabled: hide text by clearing it
                storedText = Text;
                Text = string.Empty;
                Cursor = Cursors.Default;
            }
        }

        private void GlowTimer_Tick(object sender, EventArgs e)
        {
            glowPhase = (glowPhase + 1) % 100;

            // Calculate glow intensity (sin wave for smooth fade)
            double intensity = (Math.Sin(glowPhase * Math.PI / 50.0) + 1.0) / 2.0;

            int r = (int)(Theme.BorderMidnight.R + (Theme.BorderMidnight.R - Theme.BorderMidnight.R) * intensity);
            int g = (int)(Theme.BorderMidnight.G + (Theme.MidnightGlow.G - Theme.BorderMidnight.G) * intensity);
            int b = (int)(Theme.BorderMidnight.B + (Theme.MidnightGlow.B - Theme.BorderMidnight.B) * intensity);

            currentGlowColor = Color.FromArgb(r, g, b);
            FlatAppearance.BorderColor = currentGlowColor;

            Invalidate(); // Redraw for glow effect
        }

        private void AnimatedButton_MouseEnter(object sender, EventArgs e)
        {
            originalX = Left;
            isShaking = true;
            shakeTimer.Start();
            FlatAppearance.BorderColor = Theme.MidnightBright;
        }

        private void AnimatedButton_MouseLeave(object sender, EventArgs e)
        {
            isShaking = false;
            shakeTimer.Stop();
            Left = originalX; // Reset position
            FlatAppearance.BorderColor = currentGlowColor;
        }

        private void ShakeTimer_Tick(object sender, EventArgs e)
        {
            if (!isShaking) return;

            // Quick side-to-side shake (2 pixels)
            shakeOffset = (shakeOffset == 0) ? 2 : ((shakeOffset == 2) ? -2 : 0);
            Left = originalX + shakeOffset;
        }

        private void AnimatedButton_Paint(object sender, PaintEventArgs e)
        {
            // Draw subtle inner shadow for depth
            using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                e.Graphics.FillRectangle(shadowBrush, 1, 1, Width - 2, 3);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                glowTimer?.Stop();
                glowTimer?.Dispose();
                shakeTimer?.Stop();
                shakeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
