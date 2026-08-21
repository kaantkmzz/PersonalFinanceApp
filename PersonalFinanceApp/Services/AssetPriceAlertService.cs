using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class AssetPriceAlertService
    {
        private readonly AssetPriceAlertRepository _repository = new AssetPriceAlertRepository();

        public List<AssetPriceAlert> GetActiveByUser(int userId)
        {
            return _repository.GetActiveByUserId(userId);
        }

        public List<AssetPriceAlert> GetActiveBySymbol(int userId, string symbol)
        {
            return _repository.GetActiveBySymbol(userId, symbol);
        }

        public bool AddAlert(int userId, string symbol, string assetType, string direction, decimal thresholdPrice, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (direction != "above" && direction != "below")
            {
                errorMessage = "Geçersiz yön.";
                return false;
            }
            if (thresholdPrice <= 0)
            {
                errorMessage = "Eşik fiyat 0'dan büyük olmalıdır.";
                return false;
            }

            _repository.Add(new AssetPriceAlert
            {
                UserId = userId,
                Symbol = symbol,
                AssetType = assetType,
                Direction = direction,
                ThresholdPrice = thresholdPrice
            });

            return true;
        }

        public void DeleteAlert(int alertId, int userId)
        {
            _repository.Delete(alertId, userId);
        }

        public void MarkTriggered(int alertId)
        {
            _repository.MarkTriggered(alertId);
        }
    }
}
