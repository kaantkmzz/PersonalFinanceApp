using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.UI
{
    public static class UserMenu
    {
        public static void Run(User user)
        {
            bool logout = false;

            while (!logout)
            {
                Console.Clear();
                Console.WriteLine($"=== Hoş geldin, {user.Username} ===");
                Console.WriteLine("1. Gelir/Gider İşlemleri");
                Console.WriteLine("2. Kategoriler");
                Console.WriteLine("3. Aylık Rapor");
                Console.WriteLine("4. Tasarruf Hedefleri");
                Console.WriteLine("5. Notlar");
                Console.WriteLine("6. Hatırlatıcılar");
                Console.WriteLine("7. Şifre Değiştir");
                Console.WriteLine("8. Çıkış Yap");
                Console.Write("\nSeçiminiz: ");

                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("\nGelir/Gider İşlemleri — yakında eklenecek.");
                        Pause();
                        break;
                    case "2":
                        Console.WriteLine("\nKategoriler — yakında eklenecek.");
                        Pause();
                        break;
                    case "3":
                        Console.WriteLine("\nAylık Rapor — yakında eklenecek.");
                        Pause();
                        break;
                    case "4":
                        Console.WriteLine("\nTasarruf Hedefleri — yakında eklenecek.");
                        Pause();
                        break;
                    case "5":
                        Console.WriteLine("\nNotlar — yakında eklenecek.");
                        Pause();
                        break;
                    case "6":
                        Console.WriteLine("\nHatırlatıcılar — yakında eklenecek.");
                        Pause();
                        break;
                    case "7":
                        Console.WriteLine("\nŞifre Değiştir — yakında eklenecek.");
                        Pause();
                        break;
                    case "8":
                        logout = true;
                        break;
                    default:
                        Console.WriteLine("\nGeçersiz seçim.");
                        Pause();
                        break;
                }
            }
        }

        private static void Pause()
        {
            Console.WriteLine("Devam etmek için bir tuşa basın...");
            Console.ReadKey();
        }
    }
}