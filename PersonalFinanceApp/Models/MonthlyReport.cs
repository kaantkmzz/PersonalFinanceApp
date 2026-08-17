namespace PersonalFinanceApp.Models
{
    public class MonthlyReport
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal TotalGoal { get; set; }
        public decimal TotalInvest { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpense;
        public List<CategorySummary> ExpenseBreakdown { get; set; } = new List<CategorySummary>();
        public List<CategorySummary> IncomeBreakdown { get; set; } = new List<CategorySummary>();
        public List<CategorySummary> GoalBreakdown { get; set; } = new List<CategorySummary>();
        public List<CategorySummary> InvestBreakdown { get; set; } = new List<CategorySummary>();
    }
}
