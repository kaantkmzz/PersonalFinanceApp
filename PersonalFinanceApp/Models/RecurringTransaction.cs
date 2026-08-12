namespace PersonalFinanceApp.Models
{
    public class RecurringTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string Frequency { get; set; } = "monthly"; // "daily" | "weekly" | "monthly"
        public int? LastProcessedMonth { get; set; }
        public int? LastProcessedYear { get; set; }
        public DateTime? LastProcessedDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}