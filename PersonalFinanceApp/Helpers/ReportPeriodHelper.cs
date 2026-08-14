namespace PersonalFinanceApp.Helpers
{
    // Rapor periyodu (günlük/haftalık/aylık) ile ilgili ortak hesaplamalar; hem canlı rapor ekranı
    // hem de tamamlanan periyotları geçmişe kaydeden arka plan kontrolü tarafından kullanılır.
    public static class ReportPeriodHelper
    {
        public const string Daily = "daily";
        public const string Weekly = "weekly";
        public const string Monthly = "monthly";

        public static DateTime GetPeriodEnd(DateTime start, string periodType) => periodType switch
        {
            Daily => start.AddDays(1),
            Weekly => start.AddDays(7),
            _ => start.AddMonths(1)
        };

        public static string GetLabel(string periodType) => periodType switch
        {
            Daily => "Günlük",
            Weekly => "Haftalık",
            _ => "Aylık"
        };
    }
}
