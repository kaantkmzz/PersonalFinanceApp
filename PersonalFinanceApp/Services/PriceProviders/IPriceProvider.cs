using System.Threading.Tasks;

namespace PersonalFinanceApp.Services.PriceProviders
{
    public interface IPriceProvider
    {
        bool CanHandle(string assetType);

        // TL cinsinden birim fiyatı döndürür; alınamazsa null.
        Task<decimal?> GetPriceTryAsync(string symbol);
    }
}
