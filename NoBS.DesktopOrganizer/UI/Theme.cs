using System.Drawing;

namespace NoBS.DesktopOrganizer.UI
{
    public static class Theme
    {
        // ==========================================
        // RETRO 80S MIDNIGHT BLUE COLOR SCHEME
        // ==========================================
        public static Color Background => Color.FromArgb(10, 10, 10);          // Pure black background
        public static Color Panel => Color.FromArgb(18, 18, 18);               // Slightly lighter black
        public static Color DarkPanel => Color.FromArgb(8, 8, 8);              // Even darker black
        public static Color DarkGray => Color.FromArgb(25, 25, 25);            // Dark gray for panel backgrounds

        public static Color Text => Color.FromArgb(220, 220, 220);             // Light gray text
        public static Color TextMuted => Color.FromArgb(140, 140, 140);        // Muted gray
        public static Color TextBright => Color.FromArgb(255, 255, 255);       // Bright white

        public static Color BorderDark => Color.FromArgb(40, 40, 40);          // Dark gray border
        public static Color BorderMidnight => Color.FromArgb(15, 30, 70);       // Dark midnight blue border
        public static Color BorderMidnightBright => Color.FromArgb(30, 60, 120); // Brighter midnight blue

        public static Color Midnight => Color.FromArgb(20, 40, 90);             // Main midnight blue accent
        public static Color MidnightGlow => Color.FromArgb(40, 80, 160);        // Midnight blue glow
        public static Color MidnightDark => Color.FromArgb(10, 20, 50);         // Dark midnight blue shadow
        public static Color MidnightBright => Color.FromArgb(60, 120, 200);     // Bright midnight blue highlight

        public static Color Shadow => Color.FromArgb(50, 50, 50);              // Gray shadow
        public static Color ShadowDark => Color.FromArgb(25, 25, 25);          // Dark shadow

        public static Color StatusOnline => Color.FromArgb(40, 180, 40);       // Green for online
        public static Color StatusOffline => Color.FromArgb(140, 140, 140);    // Gray for offline

        public static Color Danger => Color.FromArgb(40, 80, 160);             // Danger midnight blue

        // ==========================================
        // FONTS - RETRO STYLE
        // ==========================================
        public static Font TitleFont => new Font("Segoe UI", 12f, FontStyle.Bold);
        public static Font SectionFont => new Font("Consolas", 10f, FontStyle.Bold);
        public static Font ButtonFont => new Font("Segoe UI", 9f, FontStyle.Bold);
        public static Font TextFont => new Font("Segoe UI", 9f, FontStyle.Regular);
        public static Font StatusFont => new Font("Consolas", 8f, FontStyle.Bold);
    }
}
