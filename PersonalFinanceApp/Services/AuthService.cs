using Npgsql;
using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;
using System;
using System.Text.RegularExpressions;

namespace PersonalFinanceApp.Services
{
    public class AuthService
    {
        // Basit e-posta format kontrolü için regex
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled);

        private const int MinPasswordLength = 6;

        /// <summary>
        /// Yeni kullanıcı kaydı oluşturur. Şifreyi BCrypt ile hash'leyerek kaydeder.
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

            if (password.Length < MinPasswordLength)
            {
                errorMessage = $"Şifre en az {MinPasswordLength} karakter olmalıdır.";
                return false;
            }

            // Şifreyi BCrypt ile güvenli hale getirme
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            try
            {
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

                    // Yeni kullanıcı ekleme
                    string insertQuery = @"
                        INSERT INTO users (username, email, password_hash, created_at) 
                        VALUES (@username, @email, @passwordHash, @createdAt)";

                    using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@username", username);
                        insertCmd.Parameters.AddWithValue("@email", email);
                        insertCmd.Parameters.AddWithValue("@passwordHash", hashedPassword);
                        insertCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                        insertCmd.ExecuteNonQuery();
                    }
                }

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

                    // Not: kolon adı şemamıza uygun şekilde "user_id" olarak düzeltildi
                    string selectQuery = "SELECT user_id, username, email, password_hash, created_at FROM users WHERE username = @input OR email = @input";
                    using (var cmd = new NpgsqlCommand(selectQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@input", usernameOrEmail);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Magic number yerine kolon adına göre okuma (daha güvenli ve okunabilir)
                                int idOrdinal = reader.GetOrdinal("user_id");
                                int usernameOrdinal = reader.GetOrdinal("username");
                                int emailOrdinal = reader.GetOrdinal("email");
                                int passwordHashOrdinal = reader.GetOrdinal("password_hash");
                                int createdAtOrdinal = reader.GetOrdinal("created_at");

                                string storedHash = reader.GetString(passwordHashOrdinal);

                                // BCrypt ile şifre doğrulama
                                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, storedHash);

                                if (isPasswordValid)
                                {
                                    return new User
                                    {
                                        Id = reader.GetInt32(idOrdinal),
                                        Username = reader.GetString(usernameOrdinal),
                                        Email = reader.GetString(emailOrdinal),
                                        PasswordHash = storedHash,
                                        CreatedAt = reader.GetDateTime(createdAtOrdinal)
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
    }
}