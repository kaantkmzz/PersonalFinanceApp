using System;

namespace PersonalFinanceApp.Models
{
    public class SavingsGoal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GoalName { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; } // YENİ EKLENEN SÜTUN
        public bool IsAchieved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public string? RecurringFrequency { get; set; } // null, "daily", "weekly", "monthly"
        public decimal? RecurringAmount { get; set; }
        public DateTime? LastContributionDate { get; set; }
    }
}