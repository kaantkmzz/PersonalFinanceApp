using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class ReportService
    {
        private readonly TransactionRepository _repository = new TransactionRepository();

        public MonthlyReport GenerateMonthlyReport(int userId, int year, int month)
        {
            var report = new MonthlyReport
            {
                Year = year,
                Month = month,
                TotalIncome = _repository.GetTotalByTypeAndMonth(userId, "income", year, month),
                TotalExpense = _repository.GetTotalByTypeAndMonth(userId, "expense", year, month),
                ExpenseBreakdown = _repository.GetCategoryBreakdown(userId, "expense", year, month),
                IncomeBreakdown = _repository.GetCategoryBreakdown(userId, "income", year, month)
            };

            return report;
        }

        public CategorySummary? GetTopExpenseCategory(MonthlyReport report)
        {
            return report.ExpenseBreakdown.OrderByDescending(c => c.TotalAmount).FirstOrDefault();
        }

        // Bir kategorinin toplam gidere oranını yüzde olarak hesaplar
        public double GetPercentage(decimal categoryAmount, decimal total)
        {
            if (total <= 0) return 0;
            return (double)(categoryAmount / total) * 100;
        }

        // Yüzdeye göre ASCII bar chart string'i üretir (toplam 24 karakter genişliğinde)
        public string GenerateBar(double percentage)
        {
            const int barWidth = 24;
            int filledCount = (int)Math.Round(percentage / 100 * barWidth);
            filledCount = Math.Clamp(filledCount, 0, barWidth);

            return new string('█', filledCount) + new string('░', barWidth - filledCount);
        }
    }
}