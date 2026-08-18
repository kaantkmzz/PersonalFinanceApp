using System.Linq;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Helpers
{
    // Sidebar'daki ve Profilim ekranındaki baş harf avatarı için ortak yardımcılar.
    public static class AvatarHelper
    {
        // Avatar dairesindeki baş harfler için Segoe UI Bold köşeli/sert duruyordu; Calibri daha
        // yuvarlak/yumuşak hatlara sahip ve Windows'ta varsayılan olarak kurulu.
        public const string InitialsFontFamily = "Calibri";


        // Kullanıcının seçebileceği hazır avatar renk paleti (koyu temayla uyumlu, birbirinden
        // ayırt edilebilir tonlar).
        public static readonly Color[] Palette =
        {
            Color.FromArgb(99, 102, 241),  // Indigo (varsayılan/AccentColor)
            Color.FromArgb(236, 72, 153),  // Pembe
            Color.FromArgb(239, 68, 68),   // Kırmızı
            Color.FromArgb(249, 115, 22),  // Turuncu
            Color.FromArgb(234, 179, 8),   // Sarı
            Color.FromArgb(34, 197, 94),   // Yeşil
            Color.FromArgb(20, 184, 166),  // Turkuaz
            Color.FromArgb(59, 130, 246),  // Mavi
            Color.FromArgb(168, 85, 247),  // Mor
            Color.FromArgb(107, 114, 128)  // Gri
        };

        // Ad Soyad varsa her kelimenin baş harfinden (en fazla 3 harf), yoksa kullanıcı adının ilk harfinden üretir.
        // Örn: "Kaan Takmaz" -> "KT", "Tuncay Kaan Takmaz" -> "TKT".
        public static string GetInitials(User user)
        {
            string name = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0].Length == 0) return "?";

            var tr = new System.Globalization.CultureInfo("tr-TR");
            return string.Concat(parts.Take(3).Select(p => p.Substring(0, 1).ToUpper(tr)));
        }

        // Avatar dairesindeki baş harflerin taşmadan sığması için, harf sayısına göre küçültülmüş
        // bir yazı tipi boyutu üretir (2 harf için tam boyut, 3 harf için küçültülmüş).
        public static float GetInitialsFontSize(int initialsLength, float twoCharSize)
            => initialsLength >= 3 ? twoCharSize * 0.7f : twoCharSize;

        public static Color ParseColor(string? hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex) || hex.Length != 7 || hex[0] != '#') return fallback;
            try
            {
                int r = Convert.ToInt32(hex.Substring(1, 2), 16);
                int g = Convert.ToInt32(hex.Substring(3, 2), 16);
                int b = Convert.ToInt32(hex.Substring(5, 2), 16);
                return Color.FromArgb(r, g, b);
            }
            catch
            {
                return fallback;
            }
        }

        public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
