namespace PersonalFinanceApp.Models
{
    public class MonthlyReport
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpense;
        public List<CategorySummary> ExpenseBreakdown { get; set; } = new List<CategorySummary>();
        public List<CategorySummary> IncomeBreakdown { get; set; } = new List<CategorySummary>();
    }
}