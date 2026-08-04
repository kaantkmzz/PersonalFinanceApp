using Npgsql;
using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace PersonalFinanceApp.Services
{
    public class AuthService
    {
        // Basit e-posta format kontrolü için regex
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled);
        // Test edilebilir olması için, e-posta format kontrolünü dışarı da açıyoruz (DB'ye hiç gitmiyor)
        public static bool IsValidEmailFormat(string email)
        {
            return EmailRegex.IsMatch(email);
        }

        public static string? GetEmailDomainSuggestion(string email)
        {
            return SuggestDomainCorrection(email);
        }
        private static readonly string[] CommonDomains =
        {
            "gmail.com", "hotmail.com", "outlook.com", "yahoo.com", "icloud.com", "live.com"
        };

        // İki kelime arasındaki "düzenleme mesafesini" (kaç harf farkı olduğunu) hesaplar
        private static int LevenshteinDistance(string s, string t)
        {
            int[,] d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= t.Length; j++) d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[s.Length, t.Length];
        }

        // Girilen e-postanın domain kısmı, bilinen bir sağlayıcıya (1-2 harf farkla) çok yakınsa öneri döndürür
        private static string? SuggestDomainCorrection(string email)
        {
            int atIndex = email.LastIndexOf('@');
            if (atIndex < 0 || atIndex == email.Length - 1) return null;

            string domain = email.Substring(atIndex + 1).ToLower();

            if (CommonDomains.Contains(domain)) return null; // zaten doğru yazılmış

            foreach (var known in CommonDomains)
            {
                int distance = LevenshteinDistance(domain, known);
                if (distance > 0 && distance <= 2)
                {
                    return known;
                }
            }

            return null;
        }

        private const int MinPasswordLength = 6;

        /// <summary>
        /// Yeni kullanıcı kaydı oluşturur. Şifreyi BCrypt ile hash'leyerek kaydeder
        /// ve kullanıcı için varsayılan kategorileri otomatik oluşturur.
        /// </summary>
        public bool Register(string username, string email, string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Tüm alanların doldurulması zorunludur.";
                return false;
            }

            if (!EmailRegex.IsMatch(email))
            {
                errorMessage = "Geçerli bir e-posta adresi giriniz.";
                return false;
            }
            string? domainSuggestion = SuggestDomainCorrection(email);
            if (domainSuggestion != null)
            {
                errorMessage = $"'{email}' geçerli bir e-posta gibi görünmüyor. '{domainSuggestion}' mi demek istediniz?";
                return false;
            }

            if (password.Length < MinPasswordLength)
            {
                errorMessage = $"Şifre en az {MinPasswordLength} karakter olmalıdır.";
                return false;
            }

            // Şifreyi BCrypt ile güvenli hale getirme
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            try
            {
                int newUserId;

                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Kullanıcı adı veya e-posta daha önce alınmış mı kontrolü
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username OR email = @email";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@username", username);
                        checkCmd.Parameters.AddWithValue("@email", email);

                        long count = (long)(checkCmd.ExecuteScalar() ?? 0L);
                        if (count > 0)
                        {
                            errorMessage = "Bu kullanıcı adı veya e-posta adresi zaten kullanımda.";
                            return false;
                        }
                    }

                    // Yeni kullanıcı ekleme — RETURNING ile eklenen satırın ID'sini geri alıyoruz
                    string insertQuery = @"
                        INSERT INTO users (username, email, password_hash, created_at) 
                        VALUES (@username, @email, @passwordHash, @createdAt)
                        RETURNING user_id";

                    using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@username", username);
                        insertCmd.Parameters.AddWithValue("@email", email);
                        insertCmd.Parameters.AddWithValue("@passwordHash", hashedPassword);
                        insertCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                        newUserId = (int)(insertCmd.ExecuteScalar() ?? 0);
                    }
                }

                // Kullanıcı başarıyla oluşturuldu, şimdi varsayılan kategorilerini ekleyelim
                var categoryService = new CategoryService();
                categoryService.CreateDefaultCategories(newUserId);

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Kayıt esnasında bir hata oluştu: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Kullanıcı girişi doğrular. Şifre eşleşirse Kullanıcı nesnesini döndürür.
        /// </summary>
        public User? Login(string usernameOrEmail, string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Giriş bilgileri boş bırakılamaz.";
                return null;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string selectQuery = "SELECT user_id, username, email, password_hash, created_at, onboarding_completed, hide_amounts FROM users WHERE username = @input OR email = @input";
                    using (var cmd = new NpgsqlCommand(selectQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@input", usernameOrEmail);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idOrdinal = reader.GetOrdinal("user_id");
                                int usernameOrdinal = reader.GetOrdinal("username");
                                int emailOrdinal = reader.GetOrdinal("email");
                                int passwordHashOrdinal = reader.GetOrdinal("password_hash");
                                int createdAtOrdinal = reader.GetOrdinal("created_at");
                                int onboardingOrdinal = reader.GetOrdinal("onboarding_completed");
                                int hideAmountsOrdinal = reader.GetOrdinal("hide_amounts");

                                string storedHash = reader.GetString(passwordHashOrdinal);

                                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, storedHash);

                                if (isPasswordValid)
                                {
                                    return new User
                                    {
                                        Id = reader.GetInt32(idOrdinal),
                                        Username = reader.GetString(usernameOrdinal),
                                        Email = reader.GetString(emailOrdinal),
                                        PasswordHash = storedHash,
                                        CreatedAt = reader.GetDateTime(createdAtOrdinal),
                                        OnboardingCompleted = reader.GetBoolean(onboardingOrdinal),
                                        HideAmountsEnabled = reader.GetBoolean(hideAmountsOrdinal)
                                    };
                                }
                            }
                        }
                    }
                }

                errorMessage = "Geçersiz kullanıcı adı/e-posta veya şifre.";
                return null;
            }
            catch (Exception ex)
            {
                errorMessage = $"Giriş yapılırken bir hata oluştu: {ex.Message}";
                return null;
            }
        }

        public bool ChangePassword(int userId, string currentPassword, string newPassword, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                errorMessage = "Tüm alanların doldurulması zorunludur.";
                return false;
            }

            if (newPassword.Length < MinPasswordLength)
            {
                errorMessage = $"Yeni şifre en az {MinPasswordLength} karakter olmalıdır.";
                return false;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string selectQuery = "SELECT password_hash FROM users WHERE user_id = @userId";
                    string storedHash;

                    using (var cmd = new NpgsqlCommand(selectQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        var result = cmd.ExecuteScalar();

                        if (result == null)
                        {
                            errorMessage = "Kullanıcı bulunamadı.";
                            return false;
                        }

                        storedHash = (string)result;
                    }

                    if (!BCrypt.Net.BCrypt.Verify(currentPassword, storedHash))
                    {
                        errorMessage = "Mevcut şifre yanlış.";
                        return false;
                    }

                    string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    string updateQuery = "UPDATE users SET password_hash = @newHash WHERE user_id = @userId";

                    using (var cmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@newHash", newHash);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Şifre değiştirilirken bir hata oluştu: {ex.Message}";
                return false;
            }
        }
    } // class
} // namespace