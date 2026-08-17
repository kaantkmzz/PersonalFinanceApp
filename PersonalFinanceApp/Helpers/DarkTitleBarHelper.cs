using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PersonalFinanceApp.Helpers
{
    public static class DarkTitleBarHelper
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void EnableDarkTitleBar(Form form) => SetTitleBarDarkMode(form, true);

        // Başlık çubuğunu, verilen tema durumuna göre koyu ya da açık yapar (temayı çalışırken değiştirebilen ekranlar için).
        public static void SetTitleBarDarkMode(Form form, bool dark)
        {
            try
            {
                int useDarkMode = dark ? 1 : 0;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
            }
            catch
            {
            }
        }

        // Windows'un o denetim için sistem görsel stilini (beyaz/aydınlık çerçeve) kapatır,
        // böylece denetimin kendi BackColor/ForeColor değerleri gerçekten uygulanabilir.
        public static void DisableVisualStyle(Control control)
        {
            try { SetWindowTheme(control.Handle, "", ""); } catch { }
        }

        // Panel.AutoScroll gibi yerleşik kaydırma çubuklarını (Windows 10 1809+ üzerinde) koyu
        // temaya uygun renklendirir; aksi halde koyu temada bile hep açık/beyaz görünürler.
        public static void SetScrollBarDarkMode(Control control, bool dark)
        {
            try { SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null); } catch { }
        }

        // DateTimePicker'ın açılır takvim penceresi ayrı bir sistem penceresi olduğundan,
        // açıldığı anda o pencereyi bulup koyu tema (immersive dark mode) uyguluyoruz.
        public static void EnableDarkCalendarPopup(DateTimePicker dtp)
        {
            dtp.DropDown += (s, e) =>
            {
                IntPtr hwndCal = FindWindow("SysMonthCal32", null);
                if (hwndCal != IntPtr.Zero)
                {
                    try
                    {
                        int dark = 1;
                        DwmSetWindowAttribute(hwndCal, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
                    }
                    catch { }
                }
            };
        }
    }
}