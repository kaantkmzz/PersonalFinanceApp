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

        public bool AddRecurring(int userId, string categoryName, string type, decimal amount, string? description, string frequency, out string errorMessage)
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
                IsActive = true,
                Frequency = frequency
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

        // Giriş yapıldığında çağrılır: seçilen sıklığa göre (günlük/haftalık/aylık) süresi gelmiş,
        // aktif tekrarlayan işlemleri gerçek işlem olarak ekler
        public (List<string> Added, List<string> Failed) ProcessDueRecurring(int userId)
        {
            var added = new List<string>();
            var failed = new List<string>();

            var recurringList = _repository.GetByUserId(userId).Where(r => r.IsActive).ToList();
            var today = DateTime.Today;

            var transactionService = new TransactionService();

            foreach (var r in recurringList)
            {
                if (!IsDue(r, today)) continue;

                bool success = transactionService.AddTransaction(userId, r.CategoryId, r.Amount, r.Type, r.Description, out string errorMessage);

                if (success)
                {
                    added.Add($"{r.CategoryName} ({r.Amount:0.00} ₺)");
                }
                else
                {
                    failed.Add($"{r.CategoryName}: {errorMessage}");
                }

                _repository.UpdateLastProcessedDate(r.Id, userId, today);
            }

            return (added, failed);
        }

        private static bool IsDue(RecurringTransaction r, DateTime today)
        {
            if (r.LastProcessedDate == null) return true;
            var last = r.LastProcessedDate.Value.Date;

            return r.Frequency switch
            {
                "daily" => last < today,
                "weekly" => (today - last).Days >= 7,
                _ => last.Month != today.Month || last.Year != today.Year // "monthly"
            };
        }
    }
}