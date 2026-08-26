using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Data
{
    public class CategoryRepository
    {
        public List<Category> GetByUserId(int userId)
        {
            var categories = new List<Category>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT category_id, user_id, name, type, budget_limit, color, icon, budget_alerted_year, budget_alerted_month FROM categories WHERE user_id = @userId ORDER BY type, name";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int budgetLimitOrdinal = reader.GetOrdinal("budget_limit");
                            int colorOrdinal = reader.GetOrdinal("color");
                            int iconOrdinal = reader.GetOrdinal("icon");
                            int alertedYearOrdinal = reader.GetOrdinal("budget_alerted_year");
                            int alertedMonthOrdinal = reader.GetOrdinal("budget_alerted_month");
                            categories.Add(new Category
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Type = reader.GetString(reader.GetOrdinal("type")),
                                BudgetLimit = reader.IsDBNull(budgetLimitOrdinal) ? null : reader.GetDecimal(budgetLimitOrdinal),
                                Color = reader.IsDBNull(colorOrdinal) ? null : reader.GetString(colorOrdinal),
                                Icon = reader.IsDBNull(iconOrdinal) ? null : reader.GetString(iconOrdinal),
                                BudgetAlertedYear = reader.IsDBNull(alertedYearOrdinal) ? null : reader.GetInt32(alertedYearOrdinal),
                                BudgetAlertedMonth = reader.IsDBNull(alertedMonthOrdinal) ? null : reader.GetInt32(alertedMonthOrdinal)
                            });
                        }
                    }
                }
            }

            return categories;
        }

        public int Add(Category category)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "INSERT INTO categories (user_id, name, type) VALUES (@userId, @name, @type) RETURNING category_id";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", category.UserId);
                    cmd.Parameters.AddWithValue("@name", category.Name);
                    cmd.Parameters.AddWithValue("@type", category.Type);

                    return (int)(cmd.ExecuteScalar() ?? 0);
                }
            }
        }

        // Temizleme sıklığı ayarı tarafından kullanılır: kullanıcının, aktif bir tekrarlanan işlem
        // tarafından kullanılmayan gelir/gider kategorilerini siler (tekrarlanan işlemler temizlik sonrası da
        // çalışmaya devam etmeli, bu yüzden onların bağlı olduğu kategoriler korunur).
        // "goal" ve "invest" tipindeki kategoriler hedef/yatırım verilerine ait olduğundan hariç tutulur.
        public void DeleteUnusedByRecurring(int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    DELETE FROM categories
                    WHERE user_id = @userId
                      AND type IN ('income', 'expense')
                      AND category_id NOT IN (SELECT category_id FROM recurring_transactions WHERE user_id = @userId)";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int categoryId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // userId'yi de kontrole dahil ediyoruz ki bir kullanıcı başkasının kategorisini silemesin
                string query = "DELETE FROM categories WHERE category_id = @categoryId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(int categoryId, int userId, string newName)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE categories SET name = @name WHERE category_id = @categoryId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", newName);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetBudgetLimit(int categoryId, int userId, decimal? budgetLimit)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE categories SET budget_limit = @budgetLimit WHERE category_id = @categoryId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@budgetLimit", (object?)budgetLimit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Bütçe aşım uyarısının bu ay için gösterildiğini kalıcı olarak işaretler — uygulama
        // yeniden başlatılsa bile aynı ay içinde aynı kategori için tekrar uyarılmasın diye.
        public void MarkBudgetAlerted(int categoryId, int userId, int year, int month)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE categories SET budget_alerted_year = @year, budget_alerted_month = @month WHERE category_id = @categoryId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@year", year);
                    cmd.Parameters.AddWithValue("@month", month);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetColorIcon(int categoryId, int userId, string? color, string? icon)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE categories SET color = @color, icon = @icon WHERE category_id = @categoryId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@color", (object?)color ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@icon", (object?)icon ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}