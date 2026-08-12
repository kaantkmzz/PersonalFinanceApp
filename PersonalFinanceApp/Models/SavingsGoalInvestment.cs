using System;

namespace PersonalFinanceApp.Models
{
    public class SavingsGoalInvestment
    {
        public int Id { get; set; }
        public int GoalId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime InvestedAt { get; set; }
    }
}
