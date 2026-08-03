using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Data
{
    public class ReminderRepository
    {
        public List<Reminder> GetByUserId(int userId)
        {
            var reminders = new List<Reminder>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                SELECT reminder_id, user_id, title, reminder_date, is_completed, is_notified, created_at
                FROM reminders
                WHERE user_id = @userId
                ORDER BY reminder_date ASC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reminders.Add(new Reminder
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("reminder_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Title = reader.GetString(reader.GetOrdinal("title")),
                                ReminderDate = reader.GetDateTime(reader.GetOrdinal("reminder_date")),
                                IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                                IsNotified = reader.GetBoolean(reader.GetOrdinal("is_notified")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            });
                        }
                    }
                }
            }

            return reminders;
        }

        public int Add(Reminder reminder)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO reminders (user_id, title, reminder_date, is_completed, created_at)
                    VALUES (@userId, @title, @reminderDate, FALSE, @createdAt)
                    RETURNING reminder_id";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", reminder.UserId);
                    cmd.Parameters.AddWithValue("@title", reminder.Title);
                    cmd.Parameters.AddWithValue("@reminderDate", reminder.ReminderDate);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                    return (int)(cmd.ExecuteScalar() ?? 0);
                }
            }
        }

        public void UpdateCompletedStatus(int reminderId, int userId, bool isCompleted)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE reminders SET is_completed = @isCompleted WHERE reminder_id = @reminderId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@isCompleted", isCompleted);
                    cmd.Parameters.AddWithValue("@reminderId", reminderId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int reminderId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM reminders WHERE reminder_id = @reminderId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@reminderId", reminderId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public void MarkAsNotified(int reminderId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE reminders SET is_notified = TRUE WHERE reminder_id = @reminderId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@reminderId", reminderId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Zamanı gelmiş ama henüz bildirimi gösterilmemiş, tamamlanmamış hatırlatıcıları getirir
        public List<Reminder> GetDueUnnotified(int userId, DateTime now)
        {
            var reminders = new List<Reminder>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT reminder_id, user_id, title, reminder_date, is_completed, is_notified, created_at
            FROM reminders
            WHERE user_id = @userId AND reminder_date <= @now AND is_notified = FALSE AND is_completed = FALSE";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@now", now);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reminders.Add(new Reminder
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("reminder_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Title = reader.GetString(reader.GetOrdinal("title")),
                                ReminderDate = reader.GetDateTime(reader.GetOrdinal("reminder_date")),
                                IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                                IsNotified = reader.GetBoolean(reader.GetOrdinal("is_notified")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            });
                        }
                    }
                }
            }

            return reminders;
        }
    }
}