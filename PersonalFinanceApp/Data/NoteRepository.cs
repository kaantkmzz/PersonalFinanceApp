using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Data
{
    public class NoteRepository
    {
        public List<Note> GetByUserId(int userId)
        {
            var notes = new List<Note>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT note_id, user_id, title, content, created_at
                    FROM notes
                    WHERE user_id = @userId
                    ORDER BY created_at DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notes.Add(new Note
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("note_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Title = reader.IsDBNull(reader.GetOrdinal("title")) ? string.Empty : reader.GetString(reader.GetOrdinal("title")),
                                Content = reader.IsDBNull(reader.GetOrdinal("content")) ? string.Empty : reader.GetString(reader.GetOrdinal("content")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            });
                        }
                    }
                }
            }

            return notes;
        }

        public int Add(Note note)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO notes (user_id, title, content, created_at)
                    VALUES (@userId, @title, @content, @createdAt)
                    RETURNING note_id";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", note.UserId);
                    cmd.Parameters.AddWithValue("@title", note.Title);
                    cmd.Parameters.AddWithValue("@content", note.Content);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                    return (int)(cmd.ExecuteScalar() ?? 0);
                }
            }
        }

        public void Update(int noteId, int userId, string title, string content)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE notes SET title = @title, content = @content WHERE note_id = @noteId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@content", content);
                    cmd.Parameters.AddWithValue("@noteId", noteId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int noteId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM notes WHERE note_id = @noteId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@noteId", noteId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}