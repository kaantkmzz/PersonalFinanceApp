using System;

namespace PersonalFinanceApp.Models
{
    public class UserHolding
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty; // "crypto", "currency", "gold"
        public decimal Quantity { get; set; }
        public decimal AvgCostTry { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
