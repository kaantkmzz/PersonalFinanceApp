using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class TransactionService
    {
        private readonly TransactionRepository _repository = new TransactionRepository();
        private readonly CategoryRepository _categoryRepository = new CategoryRepository();

        public List<Transaction> GetUserTransactions(int userId)
        {
            return _repository.GetByUserId(userId);
        }

        public bool AddTransaction(int userId, int categoryId, decimal amount, string type,
            string? description, DateTime transactionDate, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (amount <= 0)
            {
                errorMessage = "Tutar 0'dan büyük olmalıdır.";
                return false;
            }

            if (type != "income" && type != "expense")
            {
                errorMessage = "Geçersiz işlem tipi.";
                return false;
            }

            // Kategorinin gerçekten bu kullanıcıya ait olduğunu ve tipinin uyuştuğunu doğruluyoruz
            var userCategories = _categoryRepository.GetByUserId(userId);
            var category = userCategories.FirstOrDefault(c => c.Id == categoryId);

            if (category == null)
            {
                errorMessage = "Geçersiz kategori.";
                return false;
            }

            if (category.Type != type)
            {
                errorMessage = $"Seçilen kategori bir '{(type == "income" ? "gelir" : "gider")}' işlemi için uygun değil.";
                return false;
            }

            var transaction = new Transaction
            {
                UserId = userId,
                CategoryId = categoryId,
                Amount = amount,
                Type = type,
                Description = description,
                TransactionDate = transactionDate
            };

            _repository.Add(transaction);
            return true;
        }

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
            return true;
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

            existing.CategoryId = categoryId;
            existing.Amount = amount;
            existing.Type = type;
            existing.Description = description;
            existing.TransactionDate = transactionDate;

            _repository.Update(existing);
            return true;
        }
    }
}