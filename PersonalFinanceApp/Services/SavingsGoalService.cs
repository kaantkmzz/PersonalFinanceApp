using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class SavingsGoalService
    {
        private readonly SavingsGoalRepository _repository = new SavingsGoalRepository();
        private readonly AccountService _accountService = new AccountService();

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

        // Hedef "gerçekleşti" olarak işaretlenince, tutarı kasadan düşer
        public bool MarkAchieved(int goalId, int userId, out string errorMessage)
        {
            errorMessage = string.Empty;

            var goal = _repository.GetByUserId(userId).FirstOrDefault(g => g.Id == goalId);
            if (goal == null)
            {
                errorMessage = "Hedef bulunamadı.";
                return false;
            }

            var (_, safe) = _accountService.GetBalances(userId);
            if (goal.TargetAmount > safe)
            {
                errorMessage = "Kasanızda yeterli bakiye yok.";
                return false;
            }

            _accountService.AdjustSafeBalance(userId, -goal.TargetAmount);
            _repository.UpdateAchievedStatus(goalId, userId, true);
            return true;
        }

        // İşaret kaldırılırsa, tutar kasaya geri eklenir
        public bool UnmarkAchieved(int goalId, int userId, out string errorMessage)
        {
            errorMessage = string.Empty;

            var goal = _repository.GetByUserId(userId).FirstOrDefault(g => g.Id == goalId);
            if (goal == null)
            {
                errorMessage = "Hedef bulunamadı.";
                return false;
            }

            _accountService.AdjustSafeBalance(userId, goal.TargetAmount);
            _repository.UpdateAchievedStatus(goalId, userId, false);
            return true;
        }

        public void DeleteGoal(int goalId, int userId)
        {
            _repository.Delete(goalId, userId);
        }
    }
}