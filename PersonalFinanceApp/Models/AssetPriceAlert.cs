namespace PersonalFinanceApp.Models
{
    public class AssetPriceAlert
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty; // "above" veya "below"
        public decimal ThresholdPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastTriggeredAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
