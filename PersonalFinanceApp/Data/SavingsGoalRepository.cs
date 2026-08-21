using Npgsql;
using PersonalFinanceApp.Models;
using System;
using System.Collections.Generic;

namespace PersonalFinanceApp.Data
{
    public class SavingsGoalRepository
    {
        public List<SavingsGoal> GetByUserId(int userId)
        {
            var goals = new List<SavingsGoal>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT goal_id, user_id, goal_name, target_amount, current_amount, is_achieved, created_at,
                           due_date, recurring_frequency, recurring_amount, last_contribution_date
                    FROM savings_goals
                    WHERE user_id = @userId
                    ORDER BY created_at DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int dueDateOrdinal = reader.GetOrdinal("due_date");
                            int freqOrdinal = reader.GetOrdinal("recurring_frequency");
                            int recAmountOrdinal = reader.GetOrdinal("recurring_amount");
                            int lastContribOrdinal = reader.GetOrdinal("last_contribution_date");

                            goals.Add(new SavingsGoal
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("goal_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                GoalName = reader.GetString(reader.GetOrdinal("goal_name")),
                                TargetAmount = reader.GetDecimal(reader.GetOrdinal("target_amount")),
                                CurrentAmount = reader.GetDecimal(reader.GetOrdinal("current_amount")), // YENİ
                                IsAchieved = reader.GetBoolean(reader.GetOrdinal("is_achieved")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                DueDate = reader.IsDBNull(dueDateOrdinal) ? null : reader.GetDateTime(dueDateOrdinal),
                                RecurringFrequency = reader.IsDBNull(freqOrdinal) ? null : reader.GetString(freqOrdinal),
                                RecurringAmount = reader.IsDBNull(recAmountOrdinal) ? null : reader.GetDecimal(recAmountOrdinal),
                                LastContributionDate = reader.IsDBNull(lastContribOrdinal) ? null : reader.GetDateTime(lastContribOrdinal)
                            });
                        }
                    }
                }
            }

            return goals;
        }

        public int Add(SavingsGoal goal)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO savings_goals (user_id, goal_name, target_amount, current_amount, is_achieved, created_at)
                    VALUES (@userId, @goalName, @targetAmount, 0, FALSE, @createdAt)
                    RETURNING goal_id";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", goal.UserId);
                    cmd.Parameters.AddWithValue("@goalName", goal.GoalName);
                    cmd.Parameters.AddWithValue("@targetAmount", goal.TargetAmount);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                    return (int)(cmd.ExecuteScalar() ?? 0);
                }
            }
        }

        public void Update(SavingsGoal goal)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    UPDATE savings_goals 
                    SET goal_name = @goalName, 
                        target_amount = @targetAmount, 
                        current_amount = @currentAmount,
                        is_achieved = @isAchieved
                    WHERE goal_id = @goalId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@goalName", goal.GoalName);
                    cmd.Parameters.AddWithValue("@targetAmount", goal.TargetAmount);
                    cmd.Parameters.AddWithValue("@currentAmount", goal.CurrentAmount); // YENİ
                    cmd.Parameters.AddWithValue("@isAchieved", goal.IsAchieved);       // YENİ
                    cmd.Parameters.AddWithValue("@goalId", goal.Id);
                    cmd.Parameters.AddWithValue("@userId", goal.UserId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetRecurringSettings(int goalId, int userId, DateTime? dueDate, string? frequency, decimal? amount)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    UPDATE savings_goals
                    SET due_date = @dueDate, recurring_frequency = @frequency, recurring_amount = @amount
                    WHERE goal_id = @goalId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@dueDate", (object?)dueDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@frequency", (object?)frequency ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@amount", (object?)amount ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@goalId", goalId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateLastContributionDate(int goalId, int userId, DateTime date)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE savings_goals SET last_contribution_date = @date WHERE goal_id = @goalId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@date", date);
                    cmd.Parameters.AddWithValue("@goalId", goalId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int goalId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM savings_goals WHERE goal_id = @goalId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@goalId", goalId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AddInvestment(int goalId, int userId, decimal amount)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO savings_goal_investments (goal_id, user_id, amount, invested_at)
                    VALUES (@goalId, @userId, @amount, @investedAt)";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@goalId", goalId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@amount", amount);
                    cmd.Parameters.AddWithValue("@investedAt", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Models.SavingsGoalInvestment> GetInvestmentHistory(int goalId, int userId)
        {
            var history = new List<Models.SavingsGoalInvestment>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT investment_id, goal_id, user_id, amount, invested_at
                    FROM savings_goal_investments
                    WHERE goal_id = @goalId AND user_id = @userId
                    ORDER BY invested_at DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@goalId", goalId);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new Models.SavingsGoalInvestment
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("investment_id")),
                                GoalId = reader.GetInt32(reader.GetOrdinal("goal_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                                InvestedAt = reader.GetDateTime(reader.GetOrdinal("invested_at"))
                            });
                        }
                    }
                }
            }

            return history;
        }
    }
}