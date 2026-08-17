namespace PersonalFinanceApp.Models
{
    public class AssetCatalogItem
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty; // "crypto", "currency", "gold"
    }
}
