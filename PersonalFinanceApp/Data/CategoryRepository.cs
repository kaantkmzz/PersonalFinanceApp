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
                string query = "SELECT category_id, user_id, name, type FROM categories WHERE user_id = @userId ORDER BY type, name";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Type = reader.GetString(reader.GetOrdinal("type"))
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
    }
}