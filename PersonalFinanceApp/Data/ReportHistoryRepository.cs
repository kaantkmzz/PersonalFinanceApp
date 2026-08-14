using System.Text.Json;
using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Data
{
    public class ReportHistoryRepository
    {
        public void Insert(ReportHistoryEntry entry)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO report_history
                        (user_id, period_type, period_start, period_end, total_income, total_expense, total_goal,
                         income_breakdown_json, expense_breakdown_json, goal_breakdown_json, created_at)
                    VALUES
                        (@userId, @periodType, @periodStart, @periodEnd, @totalIncome, @totalExpense, @totalGoal,
                         @incomeJson, @expenseJson, @goalJson, @createdAt)";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", entry.UserId);
                    cmd.Parameters.AddWithValue("@periodType", entry.PeriodType);
                    cmd.Parameters.AddWithValue("@periodStart", entry.PeriodStart);
                    cmd.Parameters.AddWithValue("@periodEnd", entry.PeriodEnd);
                    cmd.Parameters.AddWithValue("@totalIncome", entry.TotalIncome);
                    cmd.Parameters.AddWithValue("@totalExpense", entry.TotalExpense);
                    cmd.Parameters.AddWithValue("@totalGoal", entry.TotalGoal);
                    cmd.Parameters.AddWithValue("@incomeJson", JsonSerializer.Serialize(entry.IncomeBreakdown));
                    cmd.Parameters.AddWithValue("@expenseJson", JsonSerializer.Serialize(entry.ExpenseBreakdown));
                    cmd.Parameters.AddWithValue("@goalJson", JsonSerializer.Serialize(entry.GoalBreakdown));
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<ReportHistoryEntry> GetByUserId(int userId)
        {
            var results = new List<ReportHistoryEntry>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT report_id, user_id, period_type, period_start, period_end,
                           total_income, total_expense, total_goal,
                           income_breakdown_json, expense_breakdown_json, goal_breakdown_json, created_at
                    FROM report_history
                    WHERE user_id = @userId
                    ORDER BY period_start DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ReportHistoryEntry
                            {
                                Id = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                PeriodType = reader.GetString(2),
                                PeriodStart = reader.GetDateTime(3),
                                PeriodEnd = reader.GetDateTime(4),
                                TotalIncome = reader.GetDecimal(5),
                                TotalExpense = reader.GetDecimal(6),
                                TotalGoal = reader.GetDecimal(7),
                                IncomeBreakdown = JsonSerializer.Deserialize<List<CategorySummary>>(reader.GetString(8)) ?? new List<CategorySummary>(),
                                ExpenseBreakdown = JsonSerializer.Deserialize<List<CategorySummary>>(reader.GetString(9)) ?? new List<CategorySummary>(),
                                GoalBreakdown = JsonSerializer.Deserialize<List<CategorySummary>>(reader.GetString(10)) ?? new List<CategorySummary>(),
                                CreatedAt = reader.GetDateTime(11)
                            });
                        }
                    }
                }
            }

            return results;
        }
    }
}
