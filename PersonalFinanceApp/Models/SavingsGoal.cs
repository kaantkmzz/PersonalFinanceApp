namespace PersonalFinanceApp.Models
{
    public class SavingsGoal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GoalName { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public bool IsAchieved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}