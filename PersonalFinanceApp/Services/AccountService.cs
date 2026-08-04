using PersonalFinanceApp.Data;

namespace PersonalFinanceApp.Services
{
    public class AccountService
    {
        private readonly AccountRepository _repository = new AccountRepository();

        public void CompleteOnboarding(int userId, decimal monthlyIncome, decimal initialSavings)
        {
            _repository.CompleteOnboarding(userId, monthlyIncome, initialSavings);
        }

        public (decimal Wallet, decimal Safe) GetBalances(int userId)
        {
            return _repository.GetBalances(userId);
        }

        public List<(string Direction, decimal Amount, DateTime CreatedAt)> GetTransferHistory(int userId)
        {
            return _repository.GetTransferHistory(userId);
        }

        public bool TransferToSafe(int userId, decimal amount, out string errorMessage)
        {
            errorMessage = string.Empty;
            var (wallet, safe) = _repository.GetBalances(userId);

            if (amount <= 0)
            {
                errorMessage = "Tutar 0'dan büyük olmalıdır.";
                return false;
            }

            if (amount > wallet)
            {
                errorMessage = "Cüzdanınızda yeterli bakiye yok.";
                return false;
            }

            _repository.UpdateBalances(userId, wallet - amount, safe + amount);
            _repository.LogTransfer(userId, "wallet_to_safe", amount);
            return true;
        }

        // Yeni bir aya geçilmişse, aylık geliri otomatik olarak cüzdana ekler. Eklendiyse true döner.
        public bool CheckAndCreditMonthlyIncome(int userId)
        {
            var (monthlyIncome, lastMonth, lastYear) = _repository.GetIncomeTrackingInfo(userId);
            int currentMonth = DateTime.Today.Month;
            int currentYear = DateTime.Today.Year;

            if (lastMonth == currentMonth && lastYear == currentYear)
            {
                return false; // bu ay için zaten eklenmiş
            }

            if (monthlyIncome > 0)
            {
                _repository.AdjustWalletBalance(userId, monthlyIncome);
            }

            _repository.UpdateLastIncomeMonth(userId, currentMonth, currentYear);
            return monthlyIncome > 0;
        }

        public bool TransferToWallet(int userId, decimal amount, out string errorMessage)
        {
            errorMessage = string.Empty;
            var (wallet, safe) = _repository.GetBalances(userId);

            if (amount <= 0) { errorMessage = "Tutar 0'dan büyük olmalıdır."; return false; }
            if (amount > safe) { errorMessage = "Kasanızda yeterli bakiye yok."; return false; }

            _repository.UpdateBalances(userId, wallet + amount, safe - amount);
            _repository.LogTransfer(userId, "safe_to_wallet", amount);
            return true;
        }

        public void AdjustWalletBalance(int userId, decimal delta)
        {
            _repository.AdjustWalletBalance(userId, delta);
        }

        public void AdjustSafeBalance(int userId, decimal delta)
        {
            _repository.AdjustSafeBalance(userId, delta);
        }

        public void SetHideAmounts(int userId, bool hideAmounts)
        {
            _repository.SetHideAmounts(userId, hideAmounts);
        }
    }
}