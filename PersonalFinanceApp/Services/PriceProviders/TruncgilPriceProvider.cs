using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PersonalFinanceApp.Services.PriceProviders
{
    // Döviz (USD/EUR) ve gram altın için Truncgil Finans API (key gerekmiyor, TL bazlı).
    // https://finans.truncgil.com/v4/today.json
    public class TruncgilPriceProvider : IPriceProvider
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private const string Url = "https://finans.truncgil.com/v4/today.json";

        // Bizim uygulama sembolümüz -> Truncgil JSON'daki anahtar. Gram altın için Truncgil'in
        // "HAS" anahtarı (Name: GRAMHASALTIN, 24 ayar gram has altın) kullanılıyor.
        private static readonly Dictionary<string, string> SymbolMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "USD", "USD" },
            { "EUR", "EUR" },
            { "GRAMALTIN", "HAS" },
        };

        private JsonDocument? _cachedDoc;
        private DateTime _cachedAt = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public bool CanHandle(string assetType) => assetType == "currency" || assetType == "gold";

        public async Task<decimal?> GetPriceTryAsync(string symbol)
        {
            if (!SymbolMap.TryGetValue(symbol, out string? jsonKey)) return null;

            var doc = await GetDocumentAsync();
            if (doc == null) return null;

            if (!doc.RootElement.TryGetProperty(jsonKey, out var entry)) return null;
            if (!entry.TryGetProperty("Selling", out var sellingEl)) return null;

            return sellingEl.GetDecimal();
        }

        private async Task<JsonDocument?> GetDocumentAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_cachedDoc != null && DateTime.UtcNow - _cachedAt < CacheDuration)
                {
                    return _cachedDoc;
                }

                using var response = await _http.GetAsync(Url);
                if (!response.IsSuccessStatusCode) return _cachedDoc;

                string json = await response.Content.ReadAsStringAsync();
                var newDoc = JsonDocument.Parse(json);

                _cachedDoc?.Dispose();
                _cachedDoc = newDoc;
                _cachedAt = DateTime.UtcNow;

                return _cachedDoc;
            }
            catch
            {
                return _cachedDoc;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
