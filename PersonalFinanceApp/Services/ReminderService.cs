using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class ReminderService
    {
        private readonly ReminderRepository _repository = new ReminderRepository();

        public List<Reminder> GetUserReminders(int userId)
        {
            return _repository.GetByUserId(userId);
        }

        public List<Reminder> GetDueUnnotified(int userId)
        {
            return _repository.GetDueUnnotified(userId, DateTime.Now);
        }

        public void MarkAsNotified(int reminderId, int userId)
        {
            _repository.MarkAsNotified(reminderId, userId);
        }

        public bool AddReminder(int userId, string title, DateTime reminderDate, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                errorMessage = "Hatırlatıcı başlığı boş olamaz.";
                return false;
            }

            _repository.Add(new Reminder
            {
                UserId = userId,
                Title = title,
                ReminderDate = reminderDate
            });

            return true;
        }

        public void SetCompleted(int reminderId, int userId, bool isCompleted)
        {
            _repository.UpdateCompletedStatus(reminderId, userId, isCompleted);
        }

        public void DeleteReminder(int reminderId, int userId)
        {
            _repository.Delete(reminderId, userId);
        }
    }
}