using System;

namespace PersonalFinanceApp.Models
{
    public class PortfolioSnapshot
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime SnapshotDate { get; set; }
        public decimal TotalValueTry { get; set; }
    }
}
