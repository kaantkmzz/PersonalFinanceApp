using System;

namespace PersonalFinanceApp.Helpers
{
    public class ConfigReader
    {
        public string ConnectionString { get; set; } = string.Empty;

        public static ConfigReader Load()
        {
            var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            return new ConfigReader
            {
                ConnectionString = !string.IsNullOrEmpty(conn)
                    ? conn
                    : "Host=localhost;Username=postgres;Password=REDACTED;Database=postgres"
            };
        }
    }
}
