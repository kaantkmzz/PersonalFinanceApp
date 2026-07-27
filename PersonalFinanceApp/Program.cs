using System;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Kişisel Finans - Auth Testi";

            var authService = new AuthService();

            Console.WriteLine("=== KULLANICI KAYIT TESTİ ===");
            Console.Write("Kullanıcı Adı: ");
            string? regUsername = Console.ReadLine();

            Console.Write("E-Posta: ");
            string? regEmail = Console.ReadLine();

            Console.Write("Şifre: ");
            string? regPassword = Console.ReadLine();

            if (authService.Register(regUsername ?? "", regEmail ?? "", regPassword ?? "", out string regError))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✅ Kayıt başarıyla oluşturuldu!\n");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Kayıt Başarısız: {regError}\n");
                Console.ResetColor();
            }

            Console.WriteLine("=== KULLANICI GİRİŞ TESTİ ===");
            Console.Write("Kullanıcı Adı veya E-Posta: ");
            string? loginInput = Console.ReadLine();

            Console.Write("Şifre: ");
            string? loginPassword = Console.ReadLine();

            var loggedInUser = authService.Login(loginInput ?? "", loginPassword ?? "", out string loginError);

            if (loggedInUser != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✅ Hoş geldiniz, {loggedInUser.Username}! (ID: {loggedInUser.Id})");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Giriş Başarısız: {loginError}");
                Console.ResetColor();
            }

            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }
    }
}