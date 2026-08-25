using Npgsql;
using PersonalFinanceApp.Models;
using System;
using System.Collections.Generic;

namespace PersonalFinanceApp.Data
{
    public class AssetRepository
    {
        public List<UserHolding> GetHoldings(int userId)
        {
            var holdings = new List<UserHolding>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT holding_id, user_id, symbol, asset_type, quantity, avg_cost_try, created_at
                    FROM user_holdings
                    WHERE user_id = @userId
                    ORDER BY created_at DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            holdings.Add(new UserHolding
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("holding_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Symbol = reader.GetString(reader.GetOrdinal("symbol")),
                                AssetType = reader.GetString(reader.GetOrdinal("asset_type")),
                                Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
                                AvgCostTry = reader.GetDecimal(reader.GetOrdinal("avg_cost_try")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            });
                        }
                    }
                }
            }

            return holdings;
        }

        public UserHolding? GetHolding(int userId, string symbol)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT holding_id, user_id, symbol, asset_type, quantity, avg_cost_try, created_at
                    FROM user_holdings
                    WHERE user_id = @userId AND symbol = @symbol";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@symbol", symbol);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserHolding
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("holding_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Symbol = reader.GetString(reader.GetOrdinal("symbol")),
                                AssetType = reader.GetString(reader.GetOrdinal("asset_type")),
                                Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
                                AvgCostTry = reader.GetDecimal(reader.GetOrdinal("avg_cost_try")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                        }
                    }
                }
            }

            return null;
        }

        // Pozisyon yoksa oluşturur, varsa miktar/ortalama maliyeti verilen değerlerle günceller (üstüne yazar, çağıran taraf hesaplar).
        public void UpsertHolding(int userId, string symbol, string assetType, decimal quantity, decimal avgCostTry)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO user_holdings (user_id, symbol, asset_type, quantity, avg_cost_try, created_at)
                    VALUES (@userId, @symbol, @assetType, @quantity, @avgCostTry, @createdAt)
                    ON CONFLICT (user_id, symbol)
                    DO UPDATE SET quantity = @quantity, avg_cost_try = @avgCostTry";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@symbol", symbol);
                    cmd.Parameters.AddWithValue("@assetType", assetType);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@avgCostTry", avgCostTry);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteHolding(int userId, string symbol)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM user_holdings WHERE user_id = @userId AND symbol = @symbol";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@symbol", symbol);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AddAssetTransaction(AssetTransaction tx)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO asset_transactions (user_id, symbol, asset_type, direction, quantity, price_try, total_try, realized_pl_try, created_at)
                    VALUES (@userId, @symbol, @assetType, @direction, @quantity, @priceTry, @totalTry, @realizedPl, @createdAt)";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", tx.UserId);
                    cmd.Parameters.AddWithValue("@symbol", tx.Symbol);
                    cmd.Parameters.AddWithValue("@assetType", tx.AssetType);
                    cmd.Parameters.AddWithValue("@direction", tx.Direction);
                    cmd.Parameters.AddWithValue("@quantity", tx.Quantity);
                    cmd.Parameters.AddWithValue("@priceTry", tx.PriceTry);
                    cmd.Parameters.AddWithValue("@totalTry", tx.TotalTry);
                    cmd.Parameters.AddWithValue("@realizedPl", (object?)tx.RealizedPlTry ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<AssetTransaction> GetAssetTransactions(int userId, string symbol)
        {
            var list = new List<AssetTransaction>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT asset_tx_id, user_id, symbol, asset_type, direction, quantity, price_try, total_try, realized_pl_try, created_at
                    FROM asset_transactions
                    WHERE user_id = @userId AND symbol = @symbol
                    ORDER BY created_at DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@symbol", symbol);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int realizedPlOrdinal = reader.GetOrdinal("realized_pl_try");
                            list.Add(new AssetTransaction
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("asset_tx_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Symbol = reader.GetString(reader.GetOrdinal("symbol")),
                                AssetType = reader.GetString(reader.GetOrdinal("asset_type")),
                                Direction = reader.GetString(reader.GetOrdinal("direction")),
                                Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
                                PriceTry = reader.GetDecimal(reader.GetOrdinal("price_try")),
                                TotalTry = reader.GetDecimal(reader.GetOrdinal("total_try")),
                                RealizedPlTry = reader.IsDBNull(realizedPlOrdinal) ? null : reader.GetDecimal(realizedPlOrdinal),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            });
                        }
                    }
                }
            }

            return list;
        }

        public void UpsertDailySnapshot(int userId, DateTime date, decimal totalValueTry)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO portfolio_value_snapshots (user_id, snapshot_date, total_value_try, created_at)
                    VALUES (@userId, @date, @totalValue, @createdAt)
                    ON CONFLICT (user_id, snapshot_date)
                    DO UPDATE SET total_value_try = @totalValue";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@date", date.Date);
                    cmd.Parameters.AddWithValue("@totalValue", totalValueTry);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<PortfolioSnapshot> GetSnapshots(int userId, DateTime fromDate)
        {
            var list = new List<PortfolioSnapshot>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT snapshot_id, user_id, snapshot_date, total_value_try
                    FROM portfolio_value_snapshots
                    WHERE user_id = @userId AND snapshot_date >= @fromDate
                    ORDER BY snapshot_date ASC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@fromDate", fromDate.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PortfolioSnapshot
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("snapshot_id")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                SnapshotDate = reader.GetDateTime(reader.GetOrdinal("snapshot_date")),
                                TotalValueTry = reader.GetDecimal(reader.GetOrdinal("total_value_try"))
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}
