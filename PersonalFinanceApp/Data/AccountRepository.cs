using Npgsql;

namespace PersonalFinanceApp.Data
{
    public class AccountRepository
    {
        public void CompleteOnboarding(int userId, decimal monthlyIncome, decimal initialSavings)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    UPDATE users
                    SET monthly_income = @monthlyIncome,
                        wallet_balance = @monthlyIncome,
                        safe_balance = @initialSavings,
                        onboarding_completed = TRUE,
                        last_income_month = @month,
                        last_income_year = @year
                    WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@monthlyIncome", monthlyIncome);
                    cmd.Parameters.AddWithValue("@initialSavings", initialSavings);
                    cmd.Parameters.AddWithValue("@month", DateTime.Today.Month);
                    cmd.Parameters.AddWithValue("@year", DateTime.Today.Year);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateBalances(int userId, decimal walletBalance, decimal safeBalance)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE users SET wallet_balance = @wallet, safe_balance = @safe WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@wallet", walletBalance);
                    cmd.Parameters.AddWithValue("@safe", safeBalance);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public (decimal Wallet, decimal Safe) GetBalances(int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT wallet_balance, safe_balance FROM users WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (reader.GetDecimal(0), reader.GetDecimal(1));
                        }
                    }
                }
            }

            return (0, 0);
        }

        public void AdjustWalletBalance(int userId, decimal delta)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE users SET wallet_balance = wallet_balance + @delta WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@delta", delta);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AdjustSafeBalance(int userId, decimal delta)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE users SET safe_balance = safe_balance + @delta WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@delta", delta);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void LogTransfer(int userId, string direction, decimal amount)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "INSERT INTO transfer_history (user_id, direction, amount, created_at) VALUES (@userId, @direction, @amount, @createdAt)";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@direction", direction);
                    cmd.Parameters.AddWithValue("@amount", amount);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<(string Direction, decimal Amount, DateTime CreatedAt)> GetTransferHistory(int userId)
        {
            var list = new List<(string, decimal, DateTime)>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT direction, amount, created_at FROM transfer_history WHERE user_id = @userId ORDER BY created_at DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetDateTime(2)));
                        }
                    }
                }
            }

            return list;
        }

        public (decimal MonthlyIncome, int? LastMonth, int? LastYear) GetIncomeTrackingInfo(int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT monthly_income, last_income_month, last_income_year FROM users WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            decimal income = reader.GetDecimal(0);
                            int? lastMonth = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                            int? lastYear = reader.IsDBNull(2) ? null : reader.GetInt32(2);
                            return (income, lastMonth, lastYear);
                        }
                    }
                }
            }

            return (0, null, null);
        }

        public void UpdateLastIncomeMonth(int userId, int month, int year)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE users SET last_income_month = @month, last_income_year = @year WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@month", month);
                    cmd.Parameters.AddWithValue("@year", year);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetHideAmounts(int userId, bool hideAmounts)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE users SET hide_amounts = @hideAmounts WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@hideAmounts", hideAmounts);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetFullName(int userId, string fullName)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE users SET full_name = @fullName WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@fullName", fullName);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetCleanupFrequency(int userId, string frequency, DateTime periodStart)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE users SET cleanup_frequency = @frequency, cleanup_period_start = @periodStart WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@frequency", frequency);
                    cmd.Parameters.AddWithValue("@periodStart", periodStart);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetCleanupPeriodStart(int userId, DateTime periodStart)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE users SET cleanup_period_start = @periodStart WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@periodStart", periodStart);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetCleanupExportBeforeClear(int userId, bool exportBeforeClear)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE users SET cleanup_export_before_clear = @exportBeforeClear WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@exportBeforeClear", exportBeforeClear);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}