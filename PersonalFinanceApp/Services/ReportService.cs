using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class ReportService
    {
        private readonly TransactionRepository _repository = new TransactionRepository();

        public MonthlyReport GenerateReport(int userId, DateTime start, DateTime end)
        {
            return new MonthlyReport
            {
                PeriodStart = start,
                PeriodEnd = end,
                TotalIncome = _repository.GetTotalByTypeAndDateRange(userId, "income", start, end),
                TotalExpense = _repository.GetTotalByTypeAndDateRange(userId, "expense", start, end),
                TotalGoal = _repository.GetTotalByTypeAndDateRange(userId, "goal", start, end),
                TotalInvest = _repository.GetTotalByTypeAndDateRange(userId, "invest", start, end),
                ExpenseBreakdown = _repository.GetCategoryBreakdownByDateRange(userId, "expense", start, end),
                IncomeBreakdown = _repository.GetCategoryBreakdownByDateRange(userId, "income", start, end),
                GoalBreakdown = _repository.GetCategoryBreakdownByDateRange(userId, "goal", start, end),
                InvestBreakdown = _repository.GetCategoryBreakdownByDateRange(userId, "invest", start, end)
            };
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
