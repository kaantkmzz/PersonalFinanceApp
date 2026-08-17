namespace PersonalFinanceApp.Models
{
    // Varlıklarım ekranında gösterilecek, canlı fiyatla zenginleştirilmiş pozisyon görünümü.
    public class AssetHoldingView
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AvgCostTry { get; set; }
        public decimal? CurrentPriceTry { get; set; }

        public decimal? CurrentValueTry => CurrentPriceTry.HasValue ? CurrentPriceTry.Value * Quantity : null;
        public decimal? ProfitLossTry => CurrentPriceTry.HasValue ? (CurrentPriceTry.Value - AvgCostTry) * Quantity : null;
        public decimal? ProfitLossPercent => (CurrentPriceTry.HasValue && AvgCostTry > 0)
            ? (CurrentPriceTry.Value - AvgCostTry) / AvgCostTry * 100
            : null;
    }
}
