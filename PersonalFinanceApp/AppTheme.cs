namespace PersonalFinanceApp
{
    public enum ThemeMode { Dark, Light }

    public static class AppTheme
    {
        public static ThemeMode Mode { get; set; } = ThemeMode.Dark;
        public static bool IsDark => Mode == ThemeMode.Dark;

        public static void Toggle() => Mode = IsDark ? ThemeMode.Light : ThemeMode.Dark;

        // --- Genel yüzeyler ---
        public static Color AppBackColor => IsDark ? Color.FromArgb(31, 34, 48) : Color.FromArgb(247, 248, 250);
        public static Color CardBackColor => IsDark ? Color.FromArgb(40, 44, 60) : Color.FromArgb(255, 255, 255);
        public static Color CardBorderColor => IsDark ? Color.FromArgb(60, 65, 85) : Color.FromArgb(224, 226, 232);
        public static Color DividerColor => IsDark ? Color.FromArgb(50, 54, 74) : Color.FromArgb(228, 230, 236);
        public static Color GridLineColor => IsDark ? Color.FromArgb(55, 60, 80) : Color.FromArgb(226, 228, 234);
        public static Color RowSeparatorColor => IsDark ? Color.FromArgb(50, 55, 75) : Color.FromArgb(230, 232, 238);
        public static Color HoverBackColor => IsDark ? Color.FromArgb(60, 65, 85) : Color.FromArgb(232, 234, 240);
        public static Color SelectedRowLineColor => IsDark ? Color.FromArgb(110, 115, 140) : Color.FromArgb(150, 155, 175);
        public static Color HeaderBackColor => IsDark ? Color.FromArgb(35, 39, 54) : Color.FromArgb(240, 241, 246);

        // --- Metin ---
        public static Color TextLight => IsDark ? Color.White : Color.FromArgb(28, 30, 38);
        public static Color TextMuted => IsDark ? Color.FromArgb(170, 173, 190) : Color.FromArgb(110, 114, 130);

        // --- Marka / durum renkleri ---
        public static Color AccentColor => Color.FromArgb(99, 102, 241);
        public static Color DangerColor => IsDark ? Color.FromArgb(220, 90, 90) : Color.FromArgb(196, 60, 60);
        public static Color SuccessColor => IsDark ? Color.FromArgb(70, 180, 120) : Color.FromArgb(40, 150, 95);

        // --- Kenar çubuğu (Sidebar) ---
        public static Color SidebarColor => IsDark ? Color.FromArgb(24, 27, 38) : Color.FromArgb(255, 255, 255);
        public static Color SidebarHoverColor => IsDark ? Color.FromArgb(45, 49, 68) : Color.FromArgb(240, 241, 246);
        public static Color SidebarActiveColor => Color.FromArgb(99, 102, 241);
        public static Color SidebarDividerColor => IsDark ? Color.FromArgb(50, 54, 74) : Color.FromArgb(232, 234, 240);
        public static Color SidebarTextColor => IsDark ? Color.Gainsboro : Color.FromArgb(60, 64, 78);
        public static Color SidebarTextActiveColor => Color.White;
        public static Color SidebarIconColor => IsDark ? Color.FromArgb(200, 203, 215) : Color.FromArgb(90, 94, 110);

        // --- Rapor grafiği ---
        public static Color IncomeColor => IsDark ? Color.FromArgb(60, 180, 110) : Color.FromArgb(35, 145, 90);
        public static Color ExpenseColor => IsDark ? Color.FromArgb(230, 100, 100) : Color.FromArgb(196, 65, 65);
        public static Color WalletColor => IsDark ? Color.FromArgb(120, 220, 150) : Color.FromArgb(30, 140, 75);
        public static Color SafeColor => IsDark ? Color.FromArgb(120, 180, 255) : Color.FromArgb(35, 100, 195);
        public static Color IdleColor => IsDark ? Color.FromArgb(230, 200, 80) : Color.FromArgb(185, 145, 20);
        public static Color SliceBorderColor => AppBackColor;

        // --- Hatırlatıcı takvimi ---
        public static Color MonthCardColor => IsDark ? Color.FromArgb(45, 50, 68) : Color.FromArgb(242, 243, 247);
        public static Color TodayColor => IsDark ? Color.FromArgb(60, 64, 90) : Color.FromArgb(226, 228, 246);
        public static Color ActiveReminderColor => Color.FromArgb(160, 220, 90, 90);
        public static Color DefaultCapsuleColor => IsDark ? Color.FromArgb(18, 255, 255, 255) : Color.FromArgb(16, 0, 0, 0);

        // --- Cüzdan / Kasa bakiye kapsülleri (üst sağdaki rozetler) ---
        public static Color WalletBadgeTextColor => IsDark ? Color.FromArgb(170, 255, 190) : Color.FromArgb(20, 120, 65);
        public static Color SafeBadgeTextColor => IsDark ? Color.FromArgb(170, 215, 255) : Color.FromArgb(20, 85, 160);

        // --- Güç menüsü (Çıkış Yap / Oturumu Kapat) ---
        public static Color LogoutColor => IsDark ? Color.FromArgb(230, 170, 100) : Color.FromArgb(170, 110, 30);
        public static Color ExitColor => IsDark ? Color.FromArgb(230, 100, 100) : Color.FromArgb(190, 60, 60);
    }
}
