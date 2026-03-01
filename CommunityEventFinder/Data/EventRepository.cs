using CommunityEventsApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace CommunityEventsApp.Data
{
    public class EventRepository : IEventRepository
    {
        private readonly string _userId = "test-user";
        private readonly IHttpClientFactory _httpFactory;

        public EventRepository(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;

            using var conn = Db.Open();
            using var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId=@u)
                INSERT INTO Users(UserId,Email)
                VALUES(@u,@e)", conn);

            cmd.Parameters.Add("@u", SqlDbType.NVarChar).Value = _userId;
            cmd.Parameters.Add("@e", SqlDbType.NVarChar).Value = "test@local";

            cmd.ExecuteNonQuery();
        }

        private SqlConnection Open() => Db.Open();

        // ================= GET EVENTS =================

        public async Task<List<EventItem>> GetEventsForCurrentMonthAsync()
        {
            //Events in this month
            //var now = DateTime.Now;
            //var start = new DateTime(now.Year, now.Month, 1);
            //var end = start.AddMonths(1);

            //Events in 1 month starting from today
            var start = DateTime.Today;
            var end = start.AddMonths(1);

            var results = new List<EventItem>();

            using var conn = Open();
            using var cmd = new SqlCommand(@"
                SELECT EventId, Source, Title, Description, Category,
                       StartTime, EndTime, VenueName, Address,
                       City, State, Zip,
                       Latitude, Longitude, Url,
                       CASE WHEN EXISTS(
                           SELECT 1 FROM Favorites f 
                           WHERE f.EventId=e.EventId AND f.UserId=@u
                       ) THEN 1 ELSE 0 END
                FROM Events e
                WHERE StartTime >= @start AND StartTime < @end
                ORDER BY StartTime ASC;", conn);

            cmd.Parameters.Add("@start", SqlDbType.DateTime).Value = start;
            cmd.Parameters.Add("@end", SqlDbType.DateTime).Value = end;
            cmd.Parameters.Add("@u", SqlDbType.NVarChar).Value = _userId;

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var ev = MapEvent(r);
                ev.IsFavorite = r.GetInt32(15) == 1;
                results.Add(ev);
            }

            return results;
        }

        public async Task<List<EventItem>> GetFavoriteEventsForCurrentMonthAsync()
        {
            var all = await GetEventsForCurrentMonthAsync();
            return all.Where(e => e.IsFavorite).ToList();
        }

        // ================= INSERT =================

        public async Task<string> InsertEventAsync(
            string title, string category, DateTime start, DateTime end,
            string venue, string address, string city, string state, string zip,
            string desc, string url)
        {
            using var conn = Open(); 

            using (var check = new SqlCommand(
                "SELECT COUNT(*) FROM Events WHERE Title=@t AND StartTime=@s",
                conn))
            {
                check.Parameters.AddWithValue("@t", title);
                check.Parameters.AddWithValue("@s", start);

                if ((int)await check.ExecuteScalarAsync() > 0)
                    throw new Exception("An event with same title and time already exists.");
            }

            var (lat, lon) = await TryGeocodeAsync(address, city, state, zip);

            //if (lat == null || lon == null)
            //    throw new Exception("Invalid or non-US address.");

            var newId = Guid.NewGuid().ToString();

            using var cmd = new SqlCommand(@"
                INSERT INTO Events
                (EventId, Source, Title, Description, Category,
                 StartTime, EndTime, VenueName, Address, City, State, Zip,
                 Latitude, Longitude, Url, CreatedByUserId)
                VALUES
                (@id,'User',@t,@d,@c,@s,@e,@v,@a,@city,@state,@zip,@lat,@lon,@url,@u)",
                conn);

            cmd.Parameters.AddWithValue("@id", newId);
            cmd.Parameters.AddWithValue("@t", title);
            cmd.Parameters.AddWithValue("@d", (object?)desc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@c", (object?)category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@s", start);
            cmd.Parameters.AddWithValue("@e", end);
            cmd.Parameters.AddWithValue("@v", (object?)venue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@a", (object?)address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@city", (object?)city ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@state", (object?)state ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@zip", (object?)zip ?? DBNull.Value);
            cmd.Parameters.Add("@lat", SqlDbType.Decimal)
              .Value = (object?)lat ?? DBNull.Value;

            cmd.Parameters.Add("@lon", SqlDbType.Decimal)
                          .Value = (object?)lon ?? DBNull.Value;

            cmd.Parameters.AddWithValue("@url",
                string.IsNullOrWhiteSpace(url) ? DBNull.Value : url);
            cmd.Parameters.AddWithValue("@u", _userId);

            await cmd.ExecuteNonQueryAsync();

            return newId;
        }

        // ================= DELETE =================

        public async Task DeleteEventAsync(string id)
        {
            using var conn = Open();

            using var cmd = new SqlCommand(
                "DELETE FROM Events WHERE EventId=@id AND CreatedByUserId=@u",
                conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@u", _userId);

            await cmd.ExecuteNonQueryAsync();
        }

        // ================= FAVORITE =================

        public async Task ToggleFavoriteAsync(string eventId)
        {
            using var conn = Open();

            using var check = new SqlCommand(
                "SELECT COUNT(*) FROM Favorites WHERE UserId=@u AND EventId=@e",
                conn);

            check.Parameters.AddWithValue("@u", _userId);
            check.Parameters.AddWithValue("@e", eventId);

            bool exists = (int)await check.ExecuteScalarAsync() > 0;

            if (exists)
            {
                using var del = new SqlCommand(
                    "DELETE FROM Favorites WHERE UserId=@u AND EventId=@e",
                    conn);

                del.Parameters.AddWithValue("@u", _userId);
                del.Parameters.AddWithValue("@e", eventId);

                await del.ExecuteNonQueryAsync();
            }
            else
            {
                using var ins = new SqlCommand(
                    "INSERT INTO Favorites(UserId,EventId) VALUES(@u,@e)",
                    conn);

                ins.Parameters.AddWithValue("@u", _userId);
                ins.Parameters.AddWithValue("@e", eventId);

                await ins.ExecuteNonQueryAsync();
            }
        }

        // ================= GEOCODE =================

        private async Task<(decimal?, decimal?)> TryGeocodeAsync(
            string address, string city, string state, string zip)
        {
            if (string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(state) ||
                string.IsNullOrWhiteSpace(zip))
                return (null, null);

            var q = $"{address}, {city}, {state} {zip}";
            var url =
                "https://nominatim.openstreetmap.org/search?format=json&limit=1&countrycodes=us&q="
                + Uri.EscapeDataString(q);

            try
            {
                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("CommunityEventsApp/1.0");

                var response = await client.GetStringAsync(url);
                var json = JsonDocument.Parse(response);

                if (json.RootElement.GetArrayLength() == 0)
                    return (null, null);

                var item = json.RootElement[0];

                decimal lat = decimal.Parse(item.GetProperty("lat").GetString()!,
                    CultureInfo.InvariantCulture);
                decimal lon = decimal.Parse(item.GetProperty("lon").GetString()!,
                    CultureInfo.InvariantCulture);

                // US bounding
                if (lat < 24 || lat > 50 || lon < -125 || lon > -66)
                    return (null, null);

                return (lat, lon);
            }
            catch
            {
                return (null, null);
            }
        }

        private EventItem MapEvent(SqlDataReader r)
        {
            return new EventItem
            {
                EventId = r.GetString(0),
                Source = r.GetString(1),
                Title = r.GetString(2),
                Description = r.IsDBNull(3) ? null : r.GetString(3),
                Category = r.IsDBNull(4) ? null : r.GetString(4),
                StartTime = r.GetDateTime(5),
                EndTime = r.IsDBNull(6) ? null : r.GetDateTime(6),
                VenueName = r.IsDBNull(7) ? null : r.GetString(7),
                Address = r.IsDBNull(8) ? null : r.GetString(8),
                City = r.IsDBNull(9) ? null : r.GetString(9),
                State = r.IsDBNull(10) ? null : r.GetString(10),
                Zip = r.IsDBNull(11) ? null : r.GetString(11),
                Latitude = r.IsDBNull(12) ? null : Convert.ToDouble(r.GetValue(12)),
                Longitude = r.IsDBNull(13) ? null : Convert.ToDouble(r.GetValue(13)),
                Url = r.IsDBNull(14) ? null : r.GetString(14)
            };
        }
    }
}
