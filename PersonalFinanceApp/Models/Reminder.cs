namespace PersonalFinanceApp.Models
{
    public class Reminder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsNotified { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}