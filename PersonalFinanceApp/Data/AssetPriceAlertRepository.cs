using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Data
{
    public class AssetPriceAlertRepository
    {
        public List<AssetPriceAlert> GetActiveByUserId(int userId)
        {
            var alerts = new List<AssetPriceAlert>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT alert_id, user_id, symbol, asset_type, direction, threshold_price, is_active, last_triggered_at, created_at
                    FROM asset_price_alerts
                    WHERE user_id = @userId AND is_active = TRUE
                    ORDER BY created_at DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alerts.Add(MapReader(reader));
                        }
                    }
                }
            }

            return alerts;
        }

        public List<AssetPriceAlert> GetActiveBySymbol(int userId, string symbol)
        {
            return GetActiveByUserId(userId).Where(a => a.Symbol == symbol).ToList();
        }

        public int Add(AssetPriceAlert alert)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO asset_price_alerts (user_id, symbol, asset_type, direction, threshold_price)
                    VALUES (@userId, @symbol, @assetType, @direction, @threshold)
                    RETURNING alert_id";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", alert.UserId);
                    cmd.Parameters.AddWithValue("@symbol", alert.Symbol);
                    cmd.Parameters.AddWithValue("@assetType", alert.AssetType);
                    cmd.Parameters.AddWithValue("@direction", alert.Direction);
                    cmd.Parameters.AddWithValue("@threshold", alert.ThresholdPrice);

                    return (int)(cmd.ExecuteScalar() ?? 0);
                }
            }
        }

        public void Delete(int alertId, int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM asset_price_alerts WHERE alert_id = @alertId AND user_id = @userId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@alertId", alertId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void MarkTriggered(int alertId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE asset_price_alerts SET last_triggered_at = @now WHERE alert_id = @alertId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@alertId", alertId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private AssetPriceAlert MapReader(NpgsqlDataReader reader)
        {
            int lastTriggeredOrdinal = reader.GetOrdinal("last_triggered_at");
            return new AssetPriceAlert
            {
                Id = reader.GetInt32(reader.GetOrdinal("alert_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                Symbol = reader.GetString(reader.GetOrdinal("symbol")),
                AssetType = reader.GetString(reader.GetOrdinal("asset_type")),
                Direction = reader.GetString(reader.GetOrdinal("direction")),
                ThresholdPrice = reader.GetDecimal(reader.GetOrdinal("threshold_price")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                LastTriggeredAt = reader.IsDBNull(lastTriggeredOrdinal) ? null : reader.GetDateTime(lastTriggeredOrdinal),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }
    }
}
