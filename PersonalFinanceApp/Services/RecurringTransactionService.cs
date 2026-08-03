using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class RecurringTransactionService
    {
        private readonly RecurringTransactionRepository _repository = new RecurringTransactionRepository();
        private readonly CategoryService _categoryService = new CategoryService();

        public List<RecurringTransaction> GetUserRecurring(int userId)
        {
            return _repository.GetByUserId(userId);
        }

        public bool AddRecurring(int userId, string categoryName, string type, decimal amount, string? description, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                errorMessage = "Lütfen bir kategori adı girin.";
                return false;
            }

            if (amount <= 0)
            {
                errorMessage = "Tutar 0'dan büyük olmalıdır.";
                return false;
            }

            var category = _categoryService.GetOrCreateCategory(userId, categoryName, type);

            _repository.Add(new RecurringTransaction
            {
                UserId = userId,
                CategoryId = category.Id,
                Amount = amount,
                Type = type,
                Description = description,
                IsActive = true
            });

            return true;
        }

        public void SetActive(int recurringId, int userId, bool isActive)
        {
            _repository.SetActive(recurringId, userId, isActive);
        }

        public void DeleteRecurring(int recurringId, int userId)
        {
            _repository.Delete(recurringId, userId);
        }

        // Giriş yapıldığında çağrılır: bu ay için henüz işlenmemiş, aktif tekrarlayan işlemleri gerçek işlem olarak ekler
        public (List<string> Added, List<string> Failed) ProcessDueRecurring(int userId)
        {
            var added = new List<string>();
            var failed = new List<string>();

            var recurringList = _repository.GetByUserId(userId).Where(r => r.IsActive).ToList();
            int currentMonth = DateTime.Today.Month;
            int currentYear = DateTime.Today.Year;

            var transactionService = new TransactionService();

            foreach (var r in recurringList)
            {
                if (r.LastProcessedMonth == currentMonth && r.LastProcessedYear == currentYear)
                {
                    continue;
                }

                bool success = transactionService.AddTransaction(userId, r.CategoryId, r.Amount, r.Type, r.Description, out string errorMessage);

                if (success)
                {
                    added.Add($"{r.CategoryName} ({r.Amount:0.00} ₺)");
                }
                else
                {
                    failed.Add($"{r.CategoryName}: {errorMessage}");
                }

                _repository.UpdateLastProcessed(r.Id, userId, currentMonth, currentYear);
            }

            return (added, failed);
        }
    }
}