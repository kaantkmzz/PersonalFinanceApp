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

        // MainForm'un ReminderTimer_Tick'inde çağrılır: tekrarlanmayan bir hatırlatıcı kalıcı olarak
        // bildirilmiş sayılır (mevcut davranış); tekrarlanan bir hatırlatıcı ise bir sonraki oluşuma
        // ilerletilip is_notified sıfırlanır, böylece seri devam eder.
        public void AdvanceOrMarkNotified(Reminder reminder)
        {
            if (reminder.Recurrence != null)
            {
                DateTime next = ComputeNextOccurrence(reminder.ReminderDate, reminder.Recurrence, DateTime.Now);
                _repository.RescheduleAndUnnotify(reminder.Id, reminder.UserId, next);
            }
            else
            {
                _repository.MarkAsNotified(reminder.Id, reminder.UserId);
            }
        }

        public bool AddReminder(int userId, string title, DateTime reminderDate, string? recurrence, out string errorMessage)
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
                ReminderDate = reminderDate,
                Recurrence = recurrence
            });

            return true;
        }

        // Tekrarlanan bir hatırlatıcıda "Tamamlandı" işaretlemek seriyi bitirmez: bu oluşum biter,
        // bir sonraki oluşum otomatik olarak (ileri tarihli, bildirilmemiş halde) oluşur. Seriyi
        // tamamen durdurmak için kullanıcı satırı siler.
        public void SetCompleted(int reminderId, int userId, bool isCompleted)
        {
            if (isCompleted)
            {
                var reminder = _repository.GetByUserId(userId).FirstOrDefault(r => r.Id == reminderId);
                if (reminder?.Recurrence != null)
                {
                    DateTime next = ComputeNextOccurrence(reminder.ReminderDate, reminder.Recurrence, DateTime.Now);
                    _repository.RescheduleAndUnnotify(reminderId, userId, next);
                    return;
                }
            }

            _repository.UpdateCompletedStatus(reminderId, userId, isCompleted);
        }

        // Balon bildirimi tıklandığında veya günün listesinden manuel olarak çağrılır.
        public void Snooze(int reminderId, int userId, int minutes = 15)
        {
            _repository.RescheduleAndUnnotify(reminderId, userId, DateTime.Now.AddMinutes(minutes));
        }

        public void DeleteReminder(int reminderId, int userId)
        {
            _repository.Delete(reminderId, userId);
        }

        // Kaçırılan oluşumları tek tek "oynatmak" yerine (ör. uygulama 5 gün kapalıyken günlük bir
        // hatırlatıcı için 5 ayrı bildirim biriktirmek) doğrudan `now`'dan sonraki en yakın oluşuma atlar.
        private static DateTime ComputeNextOccurrence(DateTime current, string recurrence, DateTime now)
        {
            DateTime next = current;

            if (recurrence == "monthly")
            {
                do { next = next.AddMonths(1); } while (next <= now);
                return next;
            }

            TimeSpan step = recurrence == "weekly" ? TimeSpan.FromDays(7) : TimeSpan.FromDays(1);
            do { next += step; } while (next <= now);
            return next;
        }
    }
}