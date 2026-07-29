using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp.UI
{
    public static class ReportMenu
    {
        private static readonly ReportService _reportService = new ReportService();

        public static void Run(User user)
        {
            Console.Clear();
            Console.WriteLine("=== Aylık Rapor ===\n");

            Console.Write("Yıl (örn: 2026): ");
            if (!int.TryParse(Console.ReadLine(), out int year))
            {
                Console.WriteLine("\nGeçersiz yıl.");
                Pause();
                return;
            }

            Console.Write("Ay (1-12): ");
            if (!int.TryParse(Console.ReadLine(), out int month) || month < 1 || month > 12)
            {
                Console.WriteLine("\nGeçersiz ay.");
                Pause();
                return;
            }

            var report = _reportService.GenerateMonthlyReport(user.Id, year, month);
            DisplayReport(report);

            Pause();
        }

        private static void DisplayReport(MonthlyReport report)
        {
            string[] monthNames = { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
                "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

            Console.Clear();
            Console.WriteLine($"=== {monthNames[report.Month]} {report.Year} Raporu ===\n");
            Console.WriteLine($"Toplam Gelir  : {report.TotalIncome,10:0.00} ₺");
            Console.WriteLine($"Toplam Gider  : {report.TotalExpense,10:0.00} ₺");
            Console.WriteLine($"Net Bakiye    : {report.NetBalance,10:0.00} ₺");

            var topCategory = _reportService.GetTopExpenseCategory(report);
            if (topCategory != null)
            {
                double topPercentage = _reportService.GetPercentage(topCategory.TotalAmount, report.TotalExpense);
                Console.WriteLine($"\nEn çok harcanan: {topCategory.CategoryName} (%{topPercentage:0.0})");
            }

            Console.WriteLine("\n--- Gider Dağılımı ---");
            if (report.ExpenseBreakdown.Count == 0)
            {
                Console.WriteLine("Bu ay için gider kaydı yok.");
            }
            else
            {
                foreach (var item in report.ExpenseBreakdown)
                {
                    double percentage = _reportService.GetPercentage(item.TotalAmount, report.TotalExpense);
                    string bar = _reportService.GenerateBar(percentage);
                    Console.WriteLine($"{item.CategoryName,-12} {bar} %{percentage,5:0.0}  ({item.TotalAmount:0.00} ₺)");
                }
            }

            Console.WriteLine("\n--- Gelir Dağılımı ---");
            if (report.IncomeBreakdown.Count == 0)
            {
                Console.WriteLine("Bu ay için gelir kaydı yok.");
            }
            else
            {
                foreach (var item in report.IncomeBreakdown)
                {
                    double percentage = _reportService.GetPercentage(item.TotalAmount, report.TotalIncome);
                    string bar = _reportService.GenerateBar(percentage);
                    Console.WriteLine($"{item.CategoryName,-12} {bar} %{percentage,5:0.0}  ({item.TotalAmount:0.00} ₺)");
                }
            }
        }

        private static void Pause()
        {
            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }
    }
}