using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PersonalFinanceApp.Services.PriceProviders
{
    // Kripto fiyatları için Binance public API (key gerekmiyor).
    public class BinancePriceProvider : IPriceProvider
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private const string BaseUrl = "https://api.binance.com/api/v3/ticker/price";

        public bool CanHandle(string assetType) => assetType == "crypto";

        public async Task<decimal?> GetPriceTryAsync(string symbol)
        {
            // Önce doğrudan TL paritesini dene (ör. BTCTRY); yoksa USDT paritesi üzerinden çevir.
            decimal? direct = await GetTickerPriceAsync(symbol + "TRY");
            if (direct.HasValue) return direct;

            decimal? usdtPrice = await GetTickerPriceAsync(symbol + "USDT");
            if (!usdtPrice.HasValue) return null;

            decimal? usdtTry = await GetTickerPriceAsync("USDTTRY");
            if (!usdtTry.HasValue) return null;

            return usdtPrice.Value * usdtTry.Value;
        }

        private async Task<decimal?> GetTickerPriceAsync(string binanceSymbol)
        {
            try
            {
                using var response = await _http.GetAsync($"{BaseUrl}?symbol={binanceSymbol}");
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("price", out var priceEl)) return null;
                if (decimal.TryParse(priceEl.GetString(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                {
                    return price;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
