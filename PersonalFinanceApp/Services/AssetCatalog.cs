using System.Collections.Generic;
using System.Linq;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    // İlk faz için küçük, küratörlü bir sembol listesi. İleride genişletilebilir.
    public static class AssetCatalog
    {
        public static readonly List<AssetCatalogItem> Items = new List<AssetCatalogItem>
        {
            new AssetCatalogItem { Symbol = "BTC", Name = "Bitcoin", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "ETH", Name = "Ethereum", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "BNB", Name = "BNB", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "SOL", Name = "Solana", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "XRP", Name = "XRP", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "DOGE", Name = "Dogecoin", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "ADA", Name = "Cardano", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "AVAX", Name = "Avalanche", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "TRX", Name = "TRON", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "DOT", Name = "Polkadot", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "LINK", Name = "Chainlink", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "LTC", Name = "Litecoin", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "SHIB", Name = "Shiba Inu", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "ATOM", Name = "Cosmos", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "UNI", Name = "Uniswap", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "NEAR", Name = "NEAR Protocol", AssetType = "crypto" },
            new AssetCatalogItem { Symbol = "ETC", Name = "Ethereum Classic", AssetType = "crypto" },

            new AssetCatalogItem { Symbol = "USD", Name = "Amerikan Doları", AssetType = "currency" },
            new AssetCatalogItem { Symbol = "EUR", Name = "Euro", AssetType = "currency" },

            new AssetCatalogItem { Symbol = "GRAMALTIN", Name = "Gram Altın", AssetType = "gold" },
        };

        public static List<AssetCatalogItem> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<AssetCatalogItem>();

            string q = query.Trim();
            return Items
                .Where(i => i.Symbol.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                         || i.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static AssetCatalogItem? FindBySymbol(string symbol)
        {
            return Items.FirstOrDefault(i => i.Symbol.Equals(symbol, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
