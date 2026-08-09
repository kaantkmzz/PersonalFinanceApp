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

            // 2. Hedefe parayı ekle
            goal.CurrentAmount += amount;

            // 3. Hedef tamamlandı mı kontrolü (Otomatik İşaretleme)
            if (goal.CurrentAmount >= goal.TargetAmount)
            {
                goal.IsAchieved = true;

                // Eğer hedefi aşan bir ödeme yapıldıysa, fazlalığı kasaya iade et
                decimal overpaid = goal.CurrentAmount - goal.TargetAmount;
                if (overpaid > 0)
                {
                    goal.CurrentAmount = goal.TargetAmount;
                    _accountService.AdjustSafeBalance(userId, overpaid);
                }
            }

            // 4. Veritabanını güncelle
            _repository.Update(goal);
            return true;
        }

        public void DeleteGoal(int goalId, int userId)
        {
            _repository.Delete(goalId, userId);
        }
    }
}