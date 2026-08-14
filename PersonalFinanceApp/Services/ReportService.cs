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
                TotalGoal = _repository.GetTotalByTypeAndMonth(userId, "goal", year, month),
                ExpenseBreakdown = _repository.GetCategoryBreakdown(userId, "expense", year, month),
                IncomeBreakdown = _repository.GetCategoryBreakdown(userId, "income", year, month),
                GoalBreakdown = _repository.GetCategoryBreakdown(userId, "goal", year, month)
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

        public MonthlyReport GetPreviousMonthReport(int userId, int year, int month)
        {
            int prevMonth = month - 1;
            int prevYear = year;
            if (prevMonth < 1)
            {
                prevMonth = 12;
                prevYear -= 1;
            }
            return GenerateMonthlyReport(userId, prevYear, prevMonth);
        }

        // Bu ay ile geçen ayı karşılaştırıp, anlamlı değişiklikleri metin olarak listeler
        public List<string> GetComparisonMessages(MonthlyReport current, MonthlyReport previous)
        {
            var messages = new List<string>();

            if (previous.TotalExpense > 0)
            {
                double overallChange = (double)((current.TotalExpense - previous.TotalExpense) / previous.TotalExpense) * 100;
                if (Math.Abs(overallChange) >= 1)
                {
                    string direction = overallChange > 0 ? "arttı" : "azaldı";
                    messages.Add($"Toplam gideriniz geçen aya göre %{Math.Abs(overallChange):0.0} {direction}.");
                }
            }

            foreach (var currentCat in current.ExpenseBreakdown)
            {
                var prevCat = previous.ExpenseBreakdown.FirstOrDefault(c => c.CategoryName == currentCat.CategoryName);

                if (prevCat != null && prevCat.TotalAmount > 0)
                {
                    double change = (double)((currentCat.TotalAmount - prevCat.TotalAmount) / prevCat.TotalAmount) * 100;
                    if (Math.Abs(change) >= 20) // sadece belirgin değişimleri gösteriyoruz
                    {
                        string direction = change > 0 ? "arttı" : "azaldı";
                        messages.Add($"{currentCat.CategoryName} kategorisi geçen aya göre %{Math.Abs(change):0.0} {direction}.");
                    }
                }
                else if (prevCat == null)
                {
                    messages.Add($"{currentCat.CategoryName} kategorisinde bu ay ilk kez harcama yaptınız.");
                }
            }

            return messages;
        }
    }
}