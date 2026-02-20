using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CommunityEventsApp.Models;

namespace CommunityEventsApp.Data
{
    public class EventRepository
    {
        // 当前登录用户ID（自动创建用户）
        private string userId;

        public EventRepository()
        {
            userId = GetOrCreateUser();
        }

        /// <summary>
        /// 自动获取或创建用户
        /// </summary>
        private string GetOrCreateUser()
        {
            string username = Environment.UserName;

            using (var conn = Db.Open())
            {
                // 查找用户
                using (var cmd = new SqlCommand(
                    "SELECT UserId FROM Users WHERE Email=@e", conn))
                {
                    cmd.Parameters.AddWithValue("@e", username);
                    var result = cmd.ExecuteScalar();

                    if (result != null)
                        return result.ToString();
                }

                // 不存在 → 创建
                using (var cmd = new SqlCommand(
                    "INSERT INTO Users(Email) OUTPUT INSERTED.UserId VALUES(@e)", conn))
                {
                    cmd.Parameters.AddWithValue("@e", username);
                    return cmd.ExecuteScalar().ToString();
                }
            }
        }

        /// <summary>
        /// 获取本月事件
        /// </summary>
        public List<EventItem> GetEventsForCurrentMonth()
        {
            var now = DateTime.Now;
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1);

            var results = new List<EventItem>();

            using (var conn = Db.Open())
            using (var cmd = new SqlCommand(@"
                SELECT EventId, Source, Title, Description, Category,
                       StartTime, EndTime, VenueName, Address,
                       Latitude, Longitude, Url
                FROM Events
                WHERE StartTime >= @start AND StartTime < @end
                ORDER BY StartTime ASC;", conn))
            {
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        results.Add(new EventItem
                        {
                            EventId = r.GetString(0),
                            Source = r.GetString(1),
                            Title = r.GetString(2),
                            Description = r.IsDBNull(3) ? null : r.GetString(3),
                            Category = r.IsDBNull(4) ? null : r.GetString(4),
                            StartTime = r.GetDateTime(5),
                            EndTime = r.IsDBNull(6) ? (DateTime?)null : r.GetDateTime(6),
                            VenueName = r.IsDBNull(7) ? null : r.GetString(7),
                            Address = r.IsDBNull(8) ? null : r.GetString(8),
                            Latitude = r.IsDBNull(9) ? (double?)null : Convert.ToDouble(r.GetValue(9)),
                            Longitude = r.IsDBNull(10) ? (double?)null : Convert.ToDouble(r.GetValue(10)),
                            Url = r.IsDBNull(11) ? null : r.GetString(11),
                        });
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 是否收藏
        /// </summary>
        public bool IsFavorite(string eventId)
        {
            using (var conn = Db.Open())
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Favorites WHERE UserId=@u AND EventId=@e", conn))
            {
                cmd.Parameters.AddWithValue("@u", userId);
                cmd.Parameters.AddWithValue("@e", eventId);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        /// <summary>
        /// 切换收藏状态
        /// </summary>
        public void ToggleFavorite(string eventId)
        {
            if (IsFavorite(eventId))
            {
                using (var conn = Db.Open())
                using (var cmd = new SqlCommand(
                    "DELETE FROM Favorites WHERE UserId=@u AND EventId=@e", conn))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    cmd.Parameters.AddWithValue("@e", eventId);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                using (var conn = Db.Open())
                using (var cmd = new SqlCommand(
                    "INSERT INTO Favorites(UserId, EventId) VALUES(@u,@e)", conn))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    cmd.Parameters.AddWithValue("@e", eventId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public List<EventItem> GetFavoriteEventsForCurrentMonth()
        {
            var now = DateTime.Now;
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1);

            var results = new List<EventItem>();

            using (var conn = Db.Open())
            using (var cmd = new SqlCommand(@"
                SELECT e.EventId, e.Source, e.Title, e.Description, e.Category,
                       e.StartTime, e.EndTime, e.VenueName, e.Address,
                       e.Latitude, e.Longitude, e.Url
                FROM Events e
                INNER JOIN Favorites f
                    ON f.EventId = e.EventId
                WHERE f.UserId = @u
                  AND e.StartTime >= @start AND e.StartTime < @end
                ORDER BY e.StartTime ASC;", conn))
            {
                cmd.Parameters.AddWithValue("@u", userId);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        results.Add(new EventItem
                        {
                            EventId = r.GetString(0),
                            Source = r.GetString(1),
                            Title = r.GetString(2),
                            Description = r.IsDBNull(3) ? null : r.GetString(3),
                            Category = r.IsDBNull(4) ? null : r.GetString(4),
                            StartTime = r.GetDateTime(5),
                            EndTime = r.IsDBNull(6) ? (DateTime?)null : r.GetDateTime(6),
                            VenueName = r.IsDBNull(7) ? null : r.GetString(7),
                            Address = r.IsDBNull(8) ? null : r.GetString(8),
                            Latitude = r.IsDBNull(9) ? (double?)null : Convert.ToDouble(r.GetValue(9)),
                            Longitude = r.IsDBNull(10) ? (double?)null : Convert.ToDouble(r.GetValue(10)),
                            Url = r.IsDBNull(11) ? null : r.GetString(11),
                        });
                    }
                }
            }
            return results;
        }

    }
}