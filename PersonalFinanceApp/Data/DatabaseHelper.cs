using Npgsql;
using PersonalFinanceApp.Helpers;
using System;

namespace PersonalFinanceApp.Data
{
    public static class DatabaseHelper
    {
        private static readonly string ConnectionString = ConfigReader.Load().ConnectionString;

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