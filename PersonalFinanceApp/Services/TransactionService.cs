using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class TransactionService
    {
        private readonly TransactionRepository _repository = new TransactionRepository();
        private readonly CategoryRepository _categoryRepository = new CategoryRepository();
        private readonly AccountService _accountService = new AccountService();   // ← BURAYA TAŞINDI

        public List<Transaction> GetUserTransactions(int userId)
        {
            return _repository.GetByUserId(userId);
        }

        

        public bool AddTransaction(int userId, int categoryId, decimal amount, string type,
    string? description, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (amount <= 0)
            {
                errorMessage = "Tutar 0'dan büyük olmalıdır.";
                return false;
            }

            if (type != "income" && type != "expense" && type != "goal" && type != "invest")
            {
                errorMessage = "Geçersiz işlem tipi.";
                return false;
            }

            var userCategories = _categoryRepository.GetByUserId(userId);
            var category = userCategories.FirstOrDefault(c => c.Id == categoryId);

            if (category == null)
            {
                errorMessage = "Geçersiz kategori.";
                return false;
            }

            if (category.Type != type)
            {
                errorMessage = $"Seçilen kategori bir '{TypeLabel(type)}' işlemi için uygun değil.";
                return false;
            }

            // Gider ise, cüzdanda yeterli bakiye var mı kontrol ediyoruz
            if (type == "expense")
            {
                var (wallet, _) = _accountService.GetBalances(userId);
                if (amount > wallet)
                {
                    errorMessage = "Cüzdanınızda yeterli bakiye yok.";
                    return false;
                }
            }

            var transaction = new Transaction
            {
                UserId = userId,
                CategoryId = categoryId,
                Amount = amount,
                Type = type,
                Description = description,
                TransactionDate = DateTime.Now
            };

            _repository.Add(transaction);

            // "goal" (hedef yatırımı) ve "invest" (varlık alım/satımı) işlemleri cüzdanı etkilemez;
            // kasadan/yatırım bakiyesinden düşme işlemi ilgili servis (SavingsGoalService, AssetService)
            // içinde ayrıca yapılıyor, burada tekrar düşülmesin.
            decimal delta = type == "income" ? amount : type == "expense" ? -amount : 0;
            if (delta != 0) _accountService.AdjustWalletBalance(userId, delta);

            return true;
        }

        private static string TypeLabel(string type) => type switch
        {
            "income" => "gelir",
            "expense" => "gider",
            "invest" => "yatırım",
            _ => "hedef"
        };

        public bool DeleteTransaction(int transactionId, int userId, out string errorMessage)
        {
            errorMessage = string.Empty;

            var existing = _repository.GetById(transactionId, userId);
            if (existing == null)
            {
                errorMessage = "İşlem bulunamadı.";
                return false;
            }

            _repository.Delete(transactionId, userId);

            decimal delta = existing.Type == "income" ? -existing.Amount : existing.Type == "expense" ? existing.Amount : 0;
            if (delta != 0) _accountService.AdjustWalletBalance(userId, delta);

            return true;
        }

        public Dictionary<int, decimal> GetCategoryTotals(int userId)
        {
            return _repository.GetTotalsByCategory(userId);
        }

        public bool UpdateTransaction(int transactionId, int userId, int categoryId, decimal amount,
            string type, string? description, DateTime transactionDate, out string errorMessage)
        {
            errorMessage = string.Empty;

            var existing = _repository.GetById(transactionId, userId);
            if (existing == null)
            {
                errorMessage = "İşlem bulunamadı.";
                return false;
            }

            if (amount <= 0)
            {
                errorMessage = "Tutar 0'dan büyük olmalıdır.";
                return false;
            }

            var userCategories = _categoryRepository.GetByUserId(userId);
            var category = userCategories.FirstOrDefault(c => c.Id == categoryId);

            if (category == null)
            {
                errorMessage = "Geçersiz kategori.";
                return false;
            }

            if (type != "income" && type != "expense" && type != "goal" && type != "invest")
            {
                errorMessage = "Geçersiz işlem tipi.";
                return false;
            }

            // Eski işlemin cüzdana etkisini geri al, yeni değerlerin etkisini uygula
            // ("goal"/"invest" tipleri cüzdanı hiç etkilemez)
            decimal reverseOldDelta = existing.Type == "income" ? -existing.Amount : existing.Type == "expense" ? existing.Amount : 0;
            decimal applyNewDelta = type == "income" ? amount : type == "expense" ? -amount : 0;

            if (type == "expense")
            {
                var (wallet, _) = _accountService.GetBalances(userId);
                if (amount > wallet + reverseOldDelta)
                {
                    errorMessage = "Cüzdanınızda yeterli bakiye yok.";
                    return false;
                }
            }

            existing.CategoryId = categoryId;
            existing.Amount = amount;
            existing.Type = type;
            existing.Description = description;
            existing.TransactionDate = transactionDate;

            _repository.Update(existing);
            _accountService.AdjustWalletBalance(userId, reverseOldDelta + applyNewDelta);

            return true;
        }
    }
}