using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PersonalFinanceApp.Helpers
{
    public static class RememberMeHelper
    {
        private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "remember.dat");

        private class StoredCredentials
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public static void Save(string username, string password)
        {
            var data = new StoredCredentials { Username = username, Password = password };
            string json = JsonSerializer.Serialize(data);
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);

            // Windows kullanıcı hesabına özel şifreleme (DPAPI) — sadece bu bilgisayarda, bu kullanıcıyla çözülebilir
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(FilePath, encryptedBytes);
        }

        public static (string Username, string Password)? Load()
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            try
            {
                byte[] encryptedBytes = File.ReadAllBytes(FilePath);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                string json = Encoding.UTF8.GetString(plainBytes);

                var data = JsonSerializer.Deserialize<StoredCredentials>(json);
                return data == null ? null : (data.Username, data.Password);
            }
            catch
            {
                // Dosya bozuksa ya da okunamıyorsa, sessizce yok say
                return null;
            }
        }

        public static void Clear()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}