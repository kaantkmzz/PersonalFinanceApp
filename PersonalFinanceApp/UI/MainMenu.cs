using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp.UI
{
    public static class MainMenu
    {
        private static readonly AuthService _authService = new AuthService();

        public static void Run()
        {
            bool exitApp = false;

            while (!exitApp)
            {
                Console.Clear();
                Console.WriteLine("=== Kişisel Finans Takip Sistemi ===");
                Console.WriteLine("1. Giriş Yap");
                Console.WriteLine("2. Kayıt Ol");
                Console.WriteLine("3. Çıkış");
                Console.Write("\nSeçiminiz: ");

                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        HandleLogin();
                        break;
                    case "2":
                        HandleRegister();
                        break;
                    case "3":
                        exitApp = true;
                        break;
                    default:
                        Console.WriteLine("Geçersiz seçim. Devam etmek için bir tuşa basın...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static void HandleLogin()
        {
            Console.Clear();
            Console.WriteLine("=== Giriş Yap ===");
            Console.Write("Kullanıcı adı veya e-posta: ");
            string usernameOrEmail = Console.ReadLine() ?? string.Empty;
            Console.Write("Şifre: ");
            string password = Console.ReadLine() ?? string.Empty;

            User? user = _authService.Login(usernameOrEmail, password, out string errorMessage);

            if (user != null)
            {
                UserMenu.Run(user);   // artık burada gerçekten çağırıyoruz
            }
            else
            {
                Console.WriteLine($"\nHata: {errorMessage}");
                Console.WriteLine("Devam etmek için bir tuşa basın...");
                Console.ReadKey();
            }
        }

        private static void HandleRegister()
        {
            Console.Clear();
            Console.WriteLine("=== Kayıt Ol ===");
            Console.Write("Kullanıcı adı: ");
            string username = Console.ReadLine() ?? string.Empty;
            Console.Write("E-posta: ");
            string email = Console.ReadLine() ?? string.Empty;
            Console.Write("Şifre: ");
            string password = Console.ReadLine() ?? string.Empty;

            bool success = _authService.Register(username, email, password, out string errorMessage);

            if (success)
            {
                Console.WriteLine("\nKayıt başarılı! Şimdi giriş yapabilirsin.");
            }
            else
            {
                Console.WriteLine($"\nHata: {errorMessage}");
            }

            Console.WriteLine("Devam etmek için bir tuşa basın...");
            Console.ReadKey();
        }
    }
}