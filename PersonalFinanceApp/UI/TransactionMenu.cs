using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp.UI
{
    public static class TransactionMenu
    {
        private static readonly TransactionService _transactionService = new TransactionService();
        private static readonly CategoryService _categoryService = new CategoryService();

        public static void Run(User user)
        {
            bool back = false;

            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== Gelir/Gider İşlemleri ===");
                Console.WriteLine("1. Yeni İşlem Ekle");
                Console.WriteLine("2. İşlemleri Listele");
                Console.WriteLine("3. İşlem Sil");
                Console.WriteLine("4. Geri Dön");
                Console.Write("\nSeçiminiz: ");

                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddTransaction(user);
                        break;
                    case "2":
                        ListTransactions(user);
                        break;
                    case "3":
                        DeleteTransaction(user);
                        break;
                    case "4":
                        back = true;
                        break;
                    default:
                        Console.WriteLine("\nGeçersiz seçim.");
                        Pause();
                        break;
                }
            }
        }

        private static void AddTransaction(User user)
        {
            Console.Clear();
            Console.WriteLine("=== Yeni İşlem Ekle ===");
            Console.WriteLine("1. Gelir");
            Console.WriteLine("2. Gider");
            Console.Write("Tip seçin: ");
            string? typeChoice = Console.ReadLine();

            string type;
            if (typeChoice == "1") type = "income";
            else if (typeChoice == "2") type = "expense";
            else
            {
                Console.WriteLine("\nGeçersiz seçim.");
                Pause();
                return;
            }

            // Kullanıcının, seçtiği tipe uygun kategorilerini listele
            var categories = _categoryService.GetUserCategoriesByType(user.Id, type);

            if (categories.Count == 0)
            {
                Console.WriteLine($"\nBu tipte ({(type == "income" ? "gelir" : "gider")}) hiç kategoriniz yok. Önce bir kategori oluşturmalısınız.");
                Pause();
                return;
            }

            Console.WriteLine("\nKategoriler:");
            foreach (var cat in categories)
            {
                Console.WriteLine($"{cat.Id}. {cat.Name}");
            }

            Console.Write("\nKategori ID seçin: ");
            if (!int.TryParse(Console.ReadLine(), out int categoryId))
            {
                Console.WriteLine("\nGeçersiz kategori ID.");
                Pause();
                return;
            }

            Console.Write("Tutar: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                Console.WriteLine("\nGeçersiz tutar.");
                Pause();
                return;
            }

            Console.Write("Tarih (gg.aa.yyyy) [boş bırakırsanız bugün]: ");
            string? dateInput = Console.ReadLine();
            DateTime transactionDate;

            if (string.IsNullOrWhiteSpace(dateInput))
            {
                transactionDate = DateTime.Today;
            }
            else if (!DateTime.TryParse(dateInput, out transactionDate))
            {
                Console.WriteLine("\nGeçersiz tarih formatı.");
                Pause();
                return;
            }

            Console.Write("Açıklama (opsiyonel): ");
            string? description = Console.ReadLine();

            bool success = _transactionService.AddTransaction(
                user.Id, categoryId, amount, type, description, transactionDate, out string errorMessage);

            if (success)
            {
                Console.WriteLine("\nİşlem başarıyla eklendi.");
            }
            else
            {
                Console.WriteLine($"\nHata: {errorMessage}");
            }

            Pause();
        }

        private static void ListTransactions(User user)
        {
            Console.Clear();
            Console.WriteLine("=== İşlemleriniz ===\n");

            var transactions = _transactionService.GetUserTransactions(user.Id);

            if (transactions.Count == 0)
            {
                Console.WriteLine("Henüz hiç işlem eklenmemiş.");
            }
            else
            {
                Console.WriteLine($"{"ID",-5} {"Tarih",-12} {"Tip",-8} {"Kategori",-15} {"Tutar",-12} Açıklama");
                Console.WriteLine(new string('-', 70));

                foreach (var t in transactions)
                {
                    string typeDisplay = t.Type == "income" ? "Gelir" : "Gider";
                    Console.WriteLine($"{t.Id,-5} {t.TransactionDate:dd.MM.yyyy,-12} {typeDisplay,-8} {t.CategoryName,-15} {t.Amount,-12:0.00} {t.Description}");
                }
            }

            Console.WriteLine();
            Pause();
        }

        private static void DeleteTransaction(User user)
        {
            ListTransactions(user);

            Console.Write("\nSilmek istediğiniz işlemin ID'sini girin: ");
            if (!int.TryParse(Console.ReadLine(), out int transactionId))
            {
                Console.WriteLine("\nGeçersiz ID.");
                Pause();
                return;
            }

            Console.Write("Bu işlemi silmek istediğinize emin misiniz? (E/H): ");
            string? confirm = Console.ReadLine();

            if (confirm?.ToUpper() == "E")
            {
                bool success = _transactionService.DeleteTransaction(transactionId, user.Id, out string errorMessage);

                Console.WriteLine(success ? "\nİşlem silindi." : $"\nHata: {errorMessage}");
            }
            else
            {
                Console.WriteLine("\nİşlem iptal edildi.");
            }

            Pause();
        }

        private static void Pause()
        {
            Console.WriteLine("Devam etmek için bir tuşa basın...");
            Console.ReadKey();
        }
    }
}