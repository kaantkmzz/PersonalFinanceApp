using System;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp.UI
{
    public static class CategoryMenu
    {
        private static readonly CategoryService _categoryService = new CategoryService();

        public static void Run(User user)
        {
            bool back = false;

            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== Kategori İşlemleri ===");
                Console.WriteLine("1. Kategorilerimi Listele");
                Console.WriteLine("2. Yeni Kategori Ekle");
                Console.WriteLine("3. Kategori Sil");
                Console.WriteLine("4. Geri Dön");
                Console.Write("\nSeçiminiz: ");

                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ListCategories(user);
                        break;
                    case "2":
                        AddCategory(user);
                        break;
                    case "3":
                        DeleteCategory(user);
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

        private static void ListCategories(User user)
        {
            Console.Clear();
            Console.WriteLine("=== Kategorileriniz ===\n");

            // Hem gelir hem de gider kategorilerini ayrı ayrı çekip gösteriyoruz
            var incomeCategories = _categoryService.GetUserCategoriesByType(user.Id, "income");
            var expenseCategories = _categoryService.GetUserCategoriesByType(user.Id, "expense");

            Console.WriteLine("--- GELİR KATEGORİLERİ ---");
            if (incomeCategories.Count == 0) Console.WriteLine("Hiç gelir kategoriniz yok.");
            foreach (var cat in incomeCategories)
            {
                Console.WriteLine($"ID: {cat.Id,-3} | {cat.Name}");
            }

            Console.WriteLine("\n--- GİDER KATEGORİLERİ ---");
            if (expenseCategories.Count == 0) Console.WriteLine("Hiç gider kategoriniz yok.");
            foreach (var cat in expenseCategories)
            {
                Console.WriteLine($"ID: {cat.Id,-3} | {cat.Name}");
            }

            Console.WriteLine();
            Pause();
        }

        private static void AddCategory(User user)
        {
            Console.Clear();
            Console.WriteLine("=== Yeni Kategori Ekle ===");

            Console.WriteLine("1. Gelir Kategorisi");
            Console.WriteLine("2. Gider Kategorisi");
            Console.Write("Tip seçin (1/2): ");
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

            Console.Write("Kategori Adı (Örn: Maaş, Market, Fatura vs.): ");
            string? name = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("\nKategori adı boş olamaz.");
                Pause();
                return;
            }

            // TransactionService'deki yapıya benzer şekilde out errorMessage kullandığını varsayarak yazdım
            bool success = _categoryService.AddCategory(user.Id, name, type, out string errorMessage);

            if (success)
            {
                Console.WriteLine("\nKategori başarıyla eklendi.");
            }
            else
            {
                Console.WriteLine($"\nHata: {errorMessage}");
            }

            Pause();
        }

        private static void DeleteCategory(User user)
        {
            ListCategories(user);

            Console.Write("\nSilmek istediğiniz kategorinin ID'sini girin: ");
            if (!int.TryParse(Console.ReadLine(), out int categoryId))
            {
                Console.WriteLine("\nGeçersiz ID.");
                Pause();
                return;
            }

            Console.Write("Bu kategoriyi silmek istediğinize emin misiniz? (Bu kategoriye ait işlemler de etkilenebilir!) (E/H): ");
            string? confirm = Console.ReadLine();

            if (confirm?.ToUpper() == "E")
            {
                try
                {
                    _categoryService.DeleteCategory(categoryId, user.Id);
                    Console.WriteLine("\nKategori başarıyla silindi.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nHata: Kategori silinemedi. {ex.Message}");
                }
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