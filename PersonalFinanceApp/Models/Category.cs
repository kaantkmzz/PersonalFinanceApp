namespace PersonalFinanceApp.Models
{
    public class Category
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "income" veya "expense"
        public decimal? BudgetLimit { get; set; } // Sadece "expense" tipi kategorilerde anlamlı
        public string? Color { get; set; } // "#RRGGBB"
        public string? Icon { get; set; } // Tek bir emoji
        // Bütçe aşım uyarısının en son hangi ay için gösterildiği — aynı ay içinde tekrar
        // uyarmamak için (uygulama yeniden başlatılsa bile) kalıcı olarak saklanır.
        public int? BudgetAlertedYear { get; set; }
        public int? BudgetAlertedMonth { get; set; }
    }
}