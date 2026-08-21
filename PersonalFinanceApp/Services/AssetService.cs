using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class AssetService
    {
        private readonly AssetRepository _repository = new AssetRepository();
        private readonly AccountService _accountService = new AccountService();
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly AssetPriceService _priceService = new AssetPriceService();

        public List<UserHolding> GetHoldings(int userId)
        {
            return _repository.GetHoldings(userId).Where(h => h.Quantity > 0).ToList();
        }

        public async Task<List<AssetHoldingView>> GetHoldingsWithLivePricesAsync(int userId)
        {
            var holdings = GetHoldings(userId);
            var views = new List<AssetHoldingView>();

            foreach (var h in holdings)
            {
                var catalogItem = AssetCatalog.FindBySymbol(h.Symbol);
                decimal? price = await _priceService.GetPriceTryAsync(h.Symbol, h.AssetType);

                views.Add(new AssetHoldingView
                {
                    Symbol = h.Symbol,
                    Name = catalogItem?.Name ?? h.Symbol,
                    AssetType = h.AssetType,
                    Quantity = h.Quantity,
                    AvgCostTry = h.AvgCostTry,
                    CurrentPriceTry = price
                });
            }

            // Rapor'daki "Portföy Değeri Gelişimi" çizgi grafiği için, fiyatları zaten çekmişken
            // bugünün toplam değerini bedavaya (ekstra çağrı gerekmeden) günlük anlık görüntü olarak kaydediyoruz.
            if (views.Count > 0)
            {
                decimal totalValue = views.Sum(v => v.CurrentValueTry ?? 0);
                _repository.UpsertDailySnapshot(userId, DateTime.Today, totalValue);
            }

            return views;
        }

        public List<PortfolioSnapshot> GetPortfolioSnapshots(int userId, int days = 30)
        {
            return _repository.GetSnapshots(userId, DateTime.Today.AddDays(-days));
        }

        // Rapor'daki "Varlık Dağılımı" sütun grafiği için: sembol -> güncel maliyet bazlı tutar (miktar × ortalama maliyet)
        public List<(string Symbol, string Name, decimal CostBasisTry)> GetHoldingsCostBasisBySymbol(int userId)
        {
            return GetHoldings(userId)
                .Select(h => (h.Symbol, AssetCatalog.FindBySymbol(h.Symbol)?.Name ?? h.Symbol, h.Quantity * h.AvgCostTry))
                .OrderByDescending(x => x.Item3)
                .ToList();
        }

        public async Task<AssetHoldingView?> GetHoldingWithLivePriceAsync(int userId, string symbol)
        {
            var holding = _repository.GetHolding(userId, symbol);
            var catalogItem = AssetCatalog.FindBySymbol(symbol);
            if (catalogItem == null) return null;

            decimal? price = await _priceService.GetPriceTryAsync(symbol, catalogItem.AssetType);

            return new AssetHoldingView
            {
                Symbol = symbol,
                Name = catalogItem.Name,
                AssetType = catalogItem.AssetType,
                Quantity = holding?.Quantity ?? 0,
                AvgCostTry = holding?.AvgCostTry ?? 0,
                CurrentPriceTry = price
            };
        }

        public List<AssetTransaction> GetTransactionHistory(int userId, string symbol)
        {
            return _repository.GetAssetTransactions(userId, symbol);
        }

        public async Task<(bool Success, string Error)> BuyAssetAsync(int userId, string symbol, decimal quantity)
        {
            if (quantity <= 0) return (false, "Miktar 0'dan büyük olmalıdır.");

            var catalogItem = AssetCatalog.FindBySymbol(symbol);
            if (catalogItem == null) return (false, "Geçersiz varlık.");

            decimal? price = await _priceService.GetPriceTryAsync(symbol, catalogItem.AssetType);
            if (!price.HasValue) return (false, "Anlık fiyat alınamadı, lütfen daha sonra tekrar deneyin.");

            decimal totalTry = quantity * price.Value;
            var (_, safeBalance) = _accountService.GetBalances(userId);
            if (totalTry > safeBalance)
            {
                return (false, $"Kasanızda yeterli bakiye yok. Mevcut: {safeBalance:N2} ₺");
            }

            _accountService.AdjustSafeBalance(userId, -totalTry);

            var existing = _repository.GetHolding(userId, symbol);
            decimal newQuantity = (existing?.Quantity ?? 0) + quantity;
            decimal newAvgCost = existing != null && existing.Quantity > 0
                ? (existing.Quantity * existing.AvgCostTry + quantity * price.Value) / newQuantity
                : price.Value;

            _repository.UpsertHolding(userId, symbol, catalogItem.AssetType, newQuantity, newAvgCost);

            _repository.AddAssetTransaction(new AssetTransaction
            {
                UserId = userId,
                Symbol = symbol,
                AssetType = catalogItem.AssetType,
                Direction = "buy",
                Quantity = quantity,
                PriceTry = price.Value,
                TotalTry = totalTry
            });

            LogInvestTransaction(userId, catalogItem.Name, $"Alım · {quantity:0.########} adet", totalTry);

            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> SellAssetAsync(int userId, string symbol, decimal quantity)
        {
            if (quantity <= 0) return (false, "Miktar 0'dan büyük olmalıdır.");

            var existing = _repository.GetHolding(userId, symbol);
            if (existing == null || existing.Quantity <= 0) return (false, "Bu varlıktan pozisyonunuz yok.");
            if (quantity > existing.Quantity) return (false, $"Elinizde bu kadar yok. Mevcut miktar: {existing.Quantity:0.########}");

            var catalogItem = AssetCatalog.FindBySymbol(symbol);
            if (catalogItem == null) return (false, "Geçersiz varlık.");

            decimal? price = await _priceService.GetPriceTryAsync(symbol, catalogItem.AssetType);
            if (!price.HasValue) return (false, "Anlık fiyat alınamadı, lütfen daha sonra tekrar deneyin.");

            decimal totalTry = quantity * price.Value;
            decimal newQuantity = existing.Quantity - quantity;

            // Gerçekleşen kâr/zarar, satış anındaki (henüz güncellenmemiş) ortalama maliyete göre hesaplanır.
            decimal realizedPl = (price.Value - existing.AvgCostTry) * quantity;

            if (newQuantity <= 0)
                _repository.DeleteHolding(userId, symbol);
            else
                _repository.UpsertHolding(userId, symbol, catalogItem.AssetType, newQuantity, existing.AvgCostTry);

            _accountService.AdjustSafeBalance(userId, totalTry);

            _repository.AddAssetTransaction(new AssetTransaction
            {
                UserId = userId,
                Symbol = symbol,
                AssetType = catalogItem.AssetType,
                Direction = "sell",
                Quantity = quantity,
                PriceTry = price.Value,
                TotalTry = totalTry,
                RealizedPlTry = realizedPl
            });

            LogInvestTransaction(userId, catalogItem.Name, $"Satış · {quantity:0.########} adet", totalTry);

            return (true, string.Empty);
        }

        // Alım/satımın İşlemler ve Kategoriler ekranlarında görünmesi için, varlığın kendi adını taşıyan
        // bir kategoride (ör. "Gram Altın", "Bitcoin") "invest" tipinde log kaydı düşer — Tip sütununda
        // "Yatırım" (bkz. TransactionControl.TypeToTr), Kategori sütununda alınan varlık görünür. Bu kayıt
        // cüzdanı/kasayı etkilemez (bkz. TransactionService), yatırım bakiyesi değişikliği yukarıda ayrıca yapılıyor.
        private void LogInvestTransaction(int userId, string categoryName, string description, decimal amount)
        {
            var category = _categoryService.GetOrCreateCategory(userId, categoryName, "invest");
            _transactionService.AddTransaction(userId, category.Id, amount, "invest", description, out _);
        }
    }
}
