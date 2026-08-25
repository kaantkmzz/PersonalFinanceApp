using System;
using System.Collections.Generic;
using System.Linq;
using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class SavingsGoalService
    {
        private readonly SavingsGoalRepository _repository = new SavingsGoalRepository();
        private readonly AccountService _accountService = new AccountService();
        private readonly CategoryService _categoryService = new CategoryService();
        private readonly TransactionService _transactionService = new TransactionService();

        // MainForm bu olaya abone olup tepside "Hedef Tamamlandı" bildirimi gösterir. Servis
        // katmanının UI/tepsi katmanına doğrudan bağımlı olmaması için event tercih edildi
        // (bu projede DI konteyneri yok, örnekler her yerde doğrudan `new ServiceX()` ile
        // oluşturuluyor — statik event, singleton kurmadan tüm örnekler arası paylaşımı sağlıyor).
        public static event Action<int, string>? GoalCompleted; // (userId, goalName)

        public List<SavingsGoal> GetUserGoals(int userId)
        {
            return _repository.GetByUserId(userId);
        }

        public bool AddGoal(int userId, string goalName, decimal targetAmount, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(goalName))
            {
                errorMessage = "Hedef adı boş olamaz.";
                return false;
            }

            if (targetAmount <= 0)
            {
                errorMessage = "Hedef tutar 0'dan büyük olmalıdır.";
                return false;
            }

            _repository.Add(new SavingsGoal
            {
                UserId = userId,
                GoalName = goalName,
                TargetAmount = targetAmount
            });

            return true;
        }

        public bool UpdateGoal(int goalId, int userId, string goalName, decimal targetAmount, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(goalName)) return false;
            if (targetAmount <= 0) return false;

            try
            {
                var goal = _repository.GetByUserId(userId).FirstOrDefault(g => g.Id == goalId);
                if (goal == null)
                {
                    errorMessage = "Hedef bulunamadı.";
                    return false;
                }

                goal.GoalName = goalName;
                goal.TargetAmount = targetAmount;

                // Hedef güncellendiğinde eğer mevcut tutar hedefi aşıyorsa/eşitse tamamlansın
                if (goal.CurrentAmount >= goal.TargetAmount)
                    goal.IsAchieved = true;
                else
                    goal.IsAchieved = false;

                _repository.Update(goal);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Hata oluştu: " + ex.Message;
                return false;
            }
        }

        // YENİ: HEDEFE YATIRIM YAPMA MANTIĞI
        public bool InvestInGoal(int goalId, int userId, decimal amount, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (amount <= 0)
            {
                errorMessage = "Yatırım tutarı 0'dan büyük olmalıdır.";
                return false;
            }

            var goal = _repository.GetByUserId(userId).FirstOrDefault(g => g.Id == goalId);
            if (goal == null)
            {
                errorMessage = "Hedef bulunamadı.";
                return false;
            }

            if (goal.IsAchieved)
            {
                errorMessage = "Bu hedef zaten tamamlanmış!";
                return false;
            }

            // Kasa (Safe) bakiye kontrolü
            var (_, safe) = _accountService.GetBalances(userId);
            if (amount > safe)
            {
                errorMessage = $"Kasanızda yeterli bakiye yok. Mevcut Kasa: {safe:N2} ₺";
                return false;
            }

            // 1. Parayı kasadan düş
            _accountService.AdjustSafeBalance(userId, -amount);

            // 2. Yatırım geçmişine kaydet
            _repository.AddInvestment(goalId, userId, amount);

            // 3. Hedefe parayı ekle
            goal.CurrentAmount += amount;
            decimal actualInvested = amount;

            // 4. Hedef tamamlandı mı kontrolü (Otomatik İşaretleme)
            bool justCompleted = false;
            if (goal.CurrentAmount >= goal.TargetAmount)
            {
                justCompleted = true;
                goal.IsAchieved = true;

                // Eğer hedefi aşan bir ödeme yapıldıysa, fazlalığı kasaya iade et
                decimal overpaid = goal.CurrentAmount - goal.TargetAmount;
                if (overpaid > 0)
                {
                    goal.CurrentAmount = goal.TargetAmount;
                    _accountService.AdjustSafeBalance(userId, overpaid);
                    actualInvested -= overpaid;
                }
            }

            // 5. Veritabanını güncelle
            _repository.Update(goal);

            // 6. İşlemler ve Kategoriler ekranlarında da görünsün diye, hedefin adını kategori
            // olarak kullanan "goal" tipinde bir işlem kaydı düş (cüzdanı etkilemez, sadece log).
            if (actualInvested > 0)
            {
                var category = _categoryService.GetOrCreateCategory(userId, goal.GoalName, "goal");
                _transactionService.AddTransaction(userId, category.Id, actualInvested, "goal",
                    $"'{goal.GoalName}' hedefine yatırım", out _);
            }

            if (justCompleted)
            {
                GoalCompleted?.Invoke(userId, goal.GoalName);
            }

            return true;
        }

        public void DeleteGoal(int goalId, int userId)
        {
            _repository.Delete(goalId, userId);
        }

        public List<Models.SavingsGoalInvestment> GetInvestmentHistory(int goalId, int userId)
        {
            return _repository.GetInvestmentHistory(goalId, userId);
        }

        public bool SetRecurringSettings(int goalId, int userId, DateTime? dueDate, string? frequency, decimal? amount, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (frequency != null && frequency != "daily" && frequency != "weekly" && frequency != "monthly")
            {
                errorMessage = "Geçersiz tekrar sıklığı.";
                return false;
            }
            if (frequency != null && (amount == null || amount <= 0))
            {
                errorMessage = "Otomatik katkı için bir tutar girin.";
                return false;
            }

            _repository.SetRecurringSettings(goalId, userId, dueDate, frequency, amount);
            return true;
        }

        // Giriş yapıldığında ve arka plan zamanlayıcısında çağrılır: süresi gelmiş otomatik hedef
        // katkılarını işler. RecurringTransactionService.ProcessDueRecurring ile aynı davranış:
        // LastContributionDate başarı/başarısızlık fark etmeksizin güncellenir (aksi halde
        // kasa bakiyesi yetersizken her tur yeniden denenip aynı hatayı üretir).
        public List<string> ProcessDueContributions(int userId)
        {
            var processed = new List<string>();
            var goals = _repository.GetByUserId(userId)
                .Where(g => !g.IsAchieved && g.RecurringFrequency != null && g.RecurringAmount != null)
                .ToList();
            var today = DateTime.Today;

            foreach (var goal in goals)
            {
                if (!IsDue(goal.LastContributionDate, goal.RecurringFrequency!, today)) continue;

                if (InvestInGoal(goal.Id, userId, goal.RecurringAmount!.Value, out _))
                {
                    processed.Add(goal.GoalName);
                }

                _repository.UpdateLastContributionDate(goal.Id, userId, today);
            }

            return processed;
        }

        private static bool IsDue(DateTime? lastContributionDate, string frequency, DateTime today)
        {
            if (lastContributionDate == null) return true;
            var last = lastContributionDate.Value.Date;

            return frequency switch
            {
                "daily" => last < today,
                "weekly" => (today - last).Days >= 7,
                _ => last.Month != today.Month || last.Year != today.Year // "monthly"
            };
        }
    }
}