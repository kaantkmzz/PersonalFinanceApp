namespace PersonalFinanceApp.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // "income" veya "expense"
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Listeleme ekranında kategori adını göstermek için (JOIN sonucu doldurulacak)
        public string? CategoryName { get; set; }
    }
}