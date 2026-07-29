using System.Text.Json;

namespace PersonalFinanceApp.Helpers
{
    public class AppConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public static class ConfigReader
    {
        public static AppConfig Load()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "config.json");

            if (!File.Exists(path))
                throw new FileNotFoundException("config.json bulunamadı. Lütfen proje köküne ekleyin.");

            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<AppConfig>(json);

            if (config == null)
                throw new InvalidOperationException("config.json okunamadı veya boş.");

            return config;
        }
    }
}