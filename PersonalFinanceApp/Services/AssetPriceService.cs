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
            new FrankfurterGoldFallbackProvider(),
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

            // Aynı varlık türünü işleyen birden fazla sağlayıcı olabilir (ör. Truncgil çöktüğünde
            // currency/gold için Frankfurter+gold-api yedeği devreye giriyor) — sırayla dene, ilk
            // başarılı sonucu kullan.
            foreach (var provider in _providers.FindAll(p => p.CanHandle(assetType)))
            {
                // Geçici bir ağ hatasıyla tek seferde başarısız olabiliyor; sıradaki sağlayıcıya
                // geçmeden önce kısa bir aradan sonra aynı sağlayıcıyı bir kez daha dene.
                decimal? price = await provider.GetPriceTryAsync(symbol);
                if (!price.HasValue)
                {
                    await Task.Delay(400);
                    price = await provider.GetPriceTryAsync(symbol);
                }

                if (price.HasValue)
                {
                    _cache[symbol] = (price.Value, DateTime.UtcNow);
                    return price;
                }
            }

            return null;
        }
    }
}
