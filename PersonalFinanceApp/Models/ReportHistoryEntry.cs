namespace PersonalFinanceApp.Models
{
    public class ReportHistoryEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PeriodType { get; set; } = "monthly";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal TotalGoal { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpense;
        public List<CategorySummary> IncomeBreakdown { get; set; } = new List<CategorySummary>();
        public List<CategorySummary> ExpenseBreakdown { get; set; } = new List<CategorySummary>();
        public List<CategorySummary> GoalBreakdown { get; set; } = new List<CategorySummary>();
        public DateTime CreatedAt { get; set; }
    }
}
