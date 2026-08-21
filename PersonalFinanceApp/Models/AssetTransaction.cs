using System;

namespace PersonalFinanceApp.Models
{
    public class AssetTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty; // "buy" veya "sell"
        public decimal Quantity { get; set; }
        public decimal PriceTry { get; set; }
        public decimal TotalTry { get; set; }
        public decimal? RealizedPlTry { get; set; } // Sadece "sell" işlemlerinde dolu
        public DateTime CreatedAt { get; set; }
    }
}
