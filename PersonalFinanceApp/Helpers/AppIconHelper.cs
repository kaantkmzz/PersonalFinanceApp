namespace PersonalFinanceApp.Helpers
{
    // Uygulama ikonunu (görev çubuğu, pencere başlığı) exe'ye gömülü ApplicationIcon'dan
    // (bkz. PersonalFinanceApp.csproj) çıkarıp Form.Icon'a atamak için kullanılır.
    public static class AppIconHelper
    {
        private static Icon? _cached;

        public static Icon? GetAppIcon()
        {
            if (_cached != null) return _cached;
            try
            {
                _cached = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                _cached = null;
            }
            return _cached;
        }
    }
}
