using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Data
{
    public class RecurringTransactionRepository
    {
        public List<RecurringTransaction> GetByUserId(int userId)
        {
            var list = new List<RecurringTransaction>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT r.recurring_id, r.user_id, r.category_id, r.amount, r.type, r.description,
                           r.is_active, r.last_processed_month, r.last_processed_year, r.created_at,
                           c.name AS category_name
                    FROM recurring_transactions r
                    JOIN categories c ON r.category_id = c.category_id
                    WHERE r.user_id = @userId
                    ORDER BY r.created_at DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new RecurringTransaction
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("recurring_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                CategoryId = reader.GetInt32(reader.GetOrdinal("category_id")),
                                CategoryName = reader.GetString(reader.GetOrdinal("category_name")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                                Type = reader.GetString(reader.GetOrdinal("type")),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                                LastProcessedMonth = reader.IsDBNull(reader.GetOrdinal("last_processed_month")) ? null : reader.GetInt32(reader.GetOrdinal("last_processed_month")),
                                LastProcessedYear = reader.IsDBNull(reader.GetOrdinal("last_processed_year")) ? null : reader.GetInt32(reader.GetOrdinal("last_processed_year")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            });
                        }
                    }
                }
            }

            return list;
        }

        public int Add(RecurringTransaction r)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO recurring_transactions (user_id, category_id, amount, type, description, is_active, created_at)
                    VALUES (@userId, @categoryId, @amount, @type, @description, TRUE, @createdAt)
                    RETURNING recurring_id";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", r.UserId);
                    cmd.Parameters.AddWithValue("@categoryId", r.CategoryId);
                    cmd.Parameters.AddWithValue("@amount", r.Amount);
                    cmd.Parameters.AddWithValue("@type", r.Type);
                    cmd.Parameters.AddWithValue("@description", (object?)r.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                    return (int)(cmd.ExecuteScalar() ?? 0);
                }
            }
        }

        public void SetActive(int recurringId, int userId, bool isActive)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE recurring_transactions SET is_active = @isActive WHERE recurring_id = @recurringId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@isActive", isActive);
                    cmd.Parameters.AddWithValue("@recurringId", recurringId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateLastProcessed(int recurringId, int userId, int month, int year)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE recurring_transactions SET last_processed_month = @month, last_processed_year = @year WHERE recurring_id = @recurringId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@month", month);
                    cmd.Parameters.AddWithValue("@year", year);
                    cmd.Parameters.AddWithValue("@recurringId", recurringId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int recurringId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM recurring_transactions WHERE recurring_id = @recurringId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@recurringId", recurringId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}