using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CommunityEventsApp.Models;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace CommunityEventsApp.Data
{
    public class EventRepository
    {
        private string userId;

        private static readonly Regex _latRegex = new Regex("\"lat\"\\s*:\\s*\"(?<lat>[-0-9.]+)\"", RegexOptions.Compiled);
        private static readonly Regex _lonRegex = new Regex("\"lon\"\\s*:\\s*\"(?<lon>[-0-9.]+)\"", RegexOptions.Compiled);

        public EventRepository()
        {
            userId = GetOrCreateUser();
        }

        private string GetOrCreateUser()
        {
            string username = Environment.UserName;

            using (var conn = Db.Open())
            {
                using (var cmd = new SqlCommand("SELECT UserId FROM Users WHERE Email=@e", conn))
                {
                    cmd.Parameters.AddWithValue("@e", username);
                    var result = cmd.ExecuteScalar();

                    if (result != null)
                        return result.ToString();
                }

                using (var cmd = new SqlCommand("INSERT INTO Users(Email) OUTPUT INSERTED.UserId VALUES(@e)", conn))
                {
                    cmd.Parameters.AddWithValue("@e", username);
                    return cmd.ExecuteScalar().ToString();
                }
            }
        }

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
                       City, State, Zip,
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

                            City = r.IsDBNull(9) ? null : r.GetString(9),
                            State = r.IsDBNull(10) ? null : r.GetString(10),
                            Zip = r.IsDBNull(11) ? null : r.GetString(11),

                            Latitude = r.IsDBNull(12) ? (double?)null : Convert.ToDouble(r.GetValue(12)),
                            Longitude = r.IsDBNull(13) ? (double?)null : Convert.ToDouble(r.GetValue(13)),
                            Url = r.IsDBNull(14) ? null : r.GetString(14),
                        });
                    }
                }
            }

            return results;
        }

        public bool IsFavorite(string eventId)
        {
            using (var conn = Db.Open())
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Favorites WHERE UserId=@u AND EventId=@e", conn))
            {
                cmd.Parameters.AddWithValue("@u", userId);
                cmd.Parameters.AddWithValue("@e", eventId);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public void ToggleFavorite(string eventId)
        {
            if (IsFavorite(eventId))
            {
                using (var conn = Db.Open())
                using (var cmd = new SqlCommand("DELETE FROM Favorites WHERE UserId=@u AND EventId=@e", conn))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    cmd.Parameters.AddWithValue("@e", eventId);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                using (var conn = Db.Open())
                using (var cmd = new SqlCommand("INSERT INTO Favorites(UserId, EventId) VALUES(@u,@e)", conn))
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
                       e.City, e.State, e.Zip,
                       e.Latitude, e.Longitude, e.Url
                FROM Events e
                INNER JOIN Favorites f ON f.EventId = e.EventId
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

                            City = r.IsDBNull(9) ? null : r.GetString(9),
                            State = r.IsDBNull(10) ? null : r.GetString(10),
                            Zip = r.IsDBNull(11) ? null : r.GetString(11),

                            Latitude = r.IsDBNull(12) ? (double?)null : Convert.ToDouble(r.GetValue(12)),
                            Longitude = r.IsDBNull(13) ? (double?)null : Convert.ToDouble(r.GetValue(13)),
                            Url = r.IsDBNull(14) ? null : r.GetString(14),
                        });
                    }
                }
            }

            return results;
        }

        public void InsertEvent(
            string title, string category, DateTime start, DateTime end,
            string venue, string address, string city, string state, string zip,
            string desc, string url)
        {
            var geo = TryGeocode(address, city, state, zip);
            var lat = geo.lat;
            var lon = geo.lon;

            using (var conn = Db.Open())
            using (var cmd = new SqlCommand(@"
                INSERT INTO Events
                (EventId, Source, Title, Description, Category,
                 StartTime, EndTime, VenueName, Address, City, State, Zip,
                 Latitude, Longitude,
                 Url, CreatedByUserId)
                VALUES
                (@id, 'User', @t, @d, @c,
                 @s, @e, @v, @a, @city, @state, @zip,
                 @lat, @lon,
                 @url, @u);", conn))
            {
                cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@t", title);
                cmd.Parameters.AddWithValue("@d", desc);
                cmd.Parameters.AddWithValue("@c", category);
                cmd.Parameters.AddWithValue("@s", start);
                cmd.Parameters.AddWithValue("@e", end);
                cmd.Parameters.AddWithValue("@v", venue);
                cmd.Parameters.AddWithValue("@a", address);
                cmd.Parameters.AddWithValue("@city", city);
                cmd.Parameters.AddWithValue("@state", state);
                cmd.Parameters.AddWithValue("@zip", zip);

                cmd.Parameters.AddWithValue("@lat", (object)lat ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@lon", (object)lon ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@url", string.IsNullOrWhiteSpace(url) ? (object)DBNull.Value : url);
                cmd.Parameters.AddWithValue("@u", userId);

                cmd.ExecuteNonQuery();
            }
        }

        private (decimal? lat, decimal? lon) TryGeocode(string address, string city, string state, string zip)
        {
            if (string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(state) ||
                string.IsNullOrWhiteSpace(zip))
                return (null, null);

            var q = string.Format("{0}, {1}, {2} {3}",
                address.Trim(), city.Trim(), state.Trim(), zip.Trim());

            try
            {
                // Thread.Sleep(1100);

                var url = "https://nominatim.openstreetmap.org/search?format=json&limit=1&q=" + Uri.EscapeDataString(q);

                using (var wc = new WebClient())
                {
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "CommunityEventsApp/1.0 (course project)");

                    var json = wc.DownloadString(url);
                    if (string.IsNullOrWhiteSpace(json)) return (null, null);

                    var mLat = _latRegex.Match(json);
                    var mLon = _lonRegex.Match(json);
                    if (!mLat.Success || !mLon.Success) return (null, null);

                    decimal lat, lon;
                    if (decimal.TryParse(mLat.Groups["lat"].Value, out lat) &&
                        decimal.TryParse(mLon.Groups["lon"].Value, out lon))
                    {
                        return (lat, lon);
                    }
                }
            }
            catch
            {
                // ignore -> return nulls
            }

            return (null, null);
        }
    }
}