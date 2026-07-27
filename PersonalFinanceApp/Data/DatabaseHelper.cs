using Npgsql;
using System;

namespace PersonalFinanceApp.Data
{
    public static class DatabaseHelper
    {
        // ⚠️ DİKKAT: "Password=1234;" kısmındaki şifreyi PostgreSQL kurulumunda koyduğun şifreyle değiştir!
        private static readonly string ConnectionString =
            "Host=localhost;Port=5432;Database=PersonalFinanceDb;Username=postgres;Password=REDACTED;";

        /// <summary>
        /// Veri tabanına yeni bir bağlantı nesnesi döndürür.
        /// </summary>
        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }

        /// <summary>
        /// PostgreSQL bağlantısının çalışıp çalışmadığını kontrol eder.
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ PostgreSQL veri tabanına başarıyla bağlandı!");
                    Console.ResetColor();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Veri tabanı bağlantı hatası: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }
    }
}