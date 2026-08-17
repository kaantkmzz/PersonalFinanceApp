using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PersonalFinanceApp.Services.PriceProviders;

namespace PersonalFinanceApp.Services
{
    public class AssetPriceService
    {
        private static readonly List<IPriceProvider> _providers = new List<IPriceProvider>
        {
            new BinancePriceProvider(),
            new TruncgilPriceProvider(),
        };

        private static readonly Dictionary<string, (decimal Price, DateTime FetchedAt)> _cache = new Dictionary<string, (decimal, DateTime)>();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        // Fiyat alınamazsa null döner; UI bu durumda "fiyat alınamadı" göstermeli.
        public async Task<decimal?> GetPriceTryAsync(string symbol, string assetType)
        {
            if (_cache.TryGetValue(symbol, out var cached) && DateTime.UtcNow - cached.FetchedAt < CacheDuration)
            {
                return cached.Price;
            }

            var provider = _providers.Find(p => p.CanHandle(assetType));
            if (provider == null) return null;

            decimal? price = await provider.GetPriceTryAsync(symbol);
            if (price.HasValue)
            {
                _cache[symbol] = (price.Value, DateTime.UtcNow);
            }

            return price;
        }
    }
}
