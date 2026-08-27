using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PersonalFinanceApp.Services.PriceProviders
{
    // Truncgil API'si zaman zaman erişilemez oluyor (bkz. TruncgilPriceProvider) — bu sağlayıcı
    // TruncgilPriceProvider'dan SONRA denenen bir yedek: döviz için Frankfurter.app (ECB verisi,
    // key gerekmiyor), gram altın için gold-api.com'un ons (XAU/USD) fiyatı + Frankfurter USD/TRY
    // kuru ile hesaplanıyor. AssetPriceService, aynı assetType'ı işleyen sağlayıcıları sırayla
    // deneyip ilk başarılı sonucu kullanıyor.
    public class FrankfurterGoldFallbackProvider : IPriceProvider
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private const decimal TroyOunceInGrams = 31.1034768m;

        public bool CanHandle(string assetType) => assetType == "currency" || assetType == "gold";

        public async Task<decimal?> GetPriceTryAsync(string symbol)
        {
            if (symbol.Equals("USD", StringComparison.OrdinalIgnoreCase) || symbol.Equals("EUR", StringComparison.OrdinalIgnoreCase))
            {
                return await GetFrankfurterRateAsync(symbol);
            }

            if (symbol.Equals("GRAMALTIN", StringComparison.OrdinalIgnoreCase))
            {
                decimal? ounceUsd = await GetGoldOunceUsdAsync();
                if (!ounceUsd.HasValue) return null;

                decimal? usdTry = await GetFrankfurterRateAsync("USD");
                if (!usdTry.HasValue) return null;

                return (ounceUsd.Value / TroyOunceInGrams) * usdTry.Value;
            }

            return null;
        }

        private async Task<decimal?> GetFrankfurterRateAsync(string fromCurrency)
        {
            try
            {
                using var response = await _http.GetAsync($"https://api.frankfurter.app/latest?from={fromCurrency}&to=TRY");
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("rates", out var rates)) return null;
                if (!rates.TryGetProperty("TRY", out var tryRate)) return null;

                return tryRate.GetDecimal();
            }
            catch
            {
                return null;
            }
        }

        private async Task<decimal?> GetGoldOunceUsdAsync()
        {
            try
            {
                using var response = await _http.GetAsync("https://api.gold-api.com/price/XAU");
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("price", out var priceEl)) return null;
                return priceEl.GetDecimal();
            }
            catch
            {
                return null;
            }
        }
    }
}
