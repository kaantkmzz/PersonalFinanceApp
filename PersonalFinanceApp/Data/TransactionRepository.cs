using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Data
{
    public class TransactionRepository
    {
        public List<Transaction> GetByUserId(int userId)
        {
            var transactions = new List<Transaction>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT t.transaction_id, t.user_id, t.category_id, t.amount, 
                           t.type, t.description, t.transaction_date, t.created_at,
                           c.name AS category_name
                    FROM transactions t
                    JOIN categories c ON t.category_id = c.category_id
                    WHERE t.user_id = @userId
                    ORDER BY t.transaction_date DESC, t.transaction_id DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(MapReaderToTransaction(reader));
                        }
                    }
                }
            }

            return transactions;
        }

        public void Add(Transaction transaction)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
                    INSERT INTO transactions (user_id, category_id, amount, type, description, transaction_date, created_at)
                    VALUES (@userId, @categoryId, @amount, @type, @description, @transactionDate, @createdAt)";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", transaction.UserId);
                    cmd.Parameters.AddWithValue("@categoryId", transaction.CategoryId);
                    cmd.Parameters.AddWithValue("@amount", transaction.Amount);
                    cmd.Parameters.AddWithValue("@type", transaction.Type);
                    cmd.Parameters.AddWithValue("@description", (object?)transaction.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@transactionDate", transaction.TransactionDate);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Kullanıcının tüm işlem geçmişini siler (temizleme sıklığı ayarı tarafından kullanılır)
        public void DeleteAllForUser(int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM transactions WHERE user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int transactionId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = "DELETE FROM transactions WHERE transaction_id = @transactionId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@transactionId", transactionId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Transaction transaction)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
                    UPDATE transactions 
                    SET category_id = @categoryId, amount = @amount, type = @type, 
                        description = @description, transaction_date = @transactionDate
                    WHERE transaction_id = @transactionId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@categoryId", transaction.CategoryId);
                    cmd.Parameters.AddWithValue("@amount", transaction.Amount);
                    cmd.Parameters.AddWithValue("@type", transaction.Type);
                    cmd.Parameters.AddWithValue("@description", (object?)transaction.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@transactionDate", transaction.TransactionDate);
                    cmd.Parameters.AddWithValue("@transactionId", transaction.Id);
                    cmd.Parameters.AddWithValue("@userId", transaction.UserId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Transaction? GetById(int transactionId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT t.transaction_id, t.user_id, t.category_id, t.amount, 
                           t.type, t.description, t.transaction_date, t.created_at,
                           c.name AS category_name
                    FROM transactions t
                    JOIN categories c ON t.category_id = c.category_id
                    WHERE t.transaction_id = @transactionId AND t.user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@transactionId", transactionId);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToTransaction(reader);
                        }
                    }
                }
            }

            return null;
        }

        // Tekrarlanan okuma mantığını tek bir yerde topluyoruz (kod tekrarını önlemek için)
        private Transaction MapReaderToTransaction(NpgsqlDataReader reader)
        {
            return new Transaction
            {
                Id = reader.GetInt32(reader.GetOrdinal("transaction_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("category_id")),
                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                Type = reader.GetString(reader.GetOrdinal("type")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                TransactionDate = reader.GetDateTime(reader.GetOrdinal("transaction_date")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                CategoryName = reader.GetString(reader.GetOrdinal("category_name"))
            };
        }

        public decimal GetTotalByTypeAndDateRange(int userId, string type, DateTime start, DateTime end)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
            SELECT COALESCE(SUM(amount), 0)
            FROM transactions
            WHERE user_id = @userId AND type = @type
              AND transaction_date >= @start AND transaction_date < @end";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    return (decimal)(cmd.ExecuteScalar() ?? 0m);
                }
            }
        }

        public List<CategorySummary> GetCategoryBreakdownByDateRange(int userId, string type, DateTime start, DateTime end)
        {
            var results = new List<CategorySummary>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
            SELECT c.name AS category_name, SUM(t.amount) AS total_amount
            FROM transactions t
            JOIN categories c ON t.category_id = c.category_id
            WHERE t.user_id = @userId AND t.type = @type
              AND t.transaction_date >= @start AND t.transaction_date < @end
            GROUP BY c.name
            ORDER BY total_amount DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new CategorySummary
                            {
                                CategoryName = reader.GetString(reader.GetOrdinal("category_name")),
                                TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount"))
                            });
                        }
                    }
                }
            }

            return results;
        }

        // Her kategori için, o kategoriye ait tüm işlemlerin toplam tutarını hesaplar
        public Dictionary<int, decimal> GetTotalsByCategory(int userId)
        {
            var totals = new Dictionary<int, decimal>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT category_id, SUM(amount) AS total
            FROM transactions
            WHERE user_id = @userId
            GROUP BY category_id";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int categoryId = reader.GetInt32(reader.GetOrdinal("category_id"));
                            decimal total = reader.GetDecimal(reader.GetOrdinal("total"));
                            totals[categoryId] = total;
                        }
                    }
                }
            }

            return totals;
        }
    }
}