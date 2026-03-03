using Community_Event_Finder.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace Community_Event_Finder.Data
{
    public class EventRepository : IEventRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpFactory;
        
        // TODO: Replace with actual logged-in user ID from User.FindFirst(ClaimTypes.NameIdentifier)
        private readonly string _userId = "test-user";

        public EventRepository(ApplicationDbContext context, IHttpClientFactory httpFactory)
        {
            _context = context;
            _httpFactory = httpFactory;
        }

        // ================= GET EVENTS =================

        public async Task<List<EventItem>> GetEventsForCurrentMonthAsync()
        {
            // Events in 1 month starting from today
            var start = DateTime.Today;
            var end = start.AddMonths(1);

            var favoriteEventIds = await _context.Favorites
                .Where(f => f.UserId == _userId)
                .Select(f => f.EventId)
                .ToListAsync();

            var results = await _context.Events
                .Where(e => e.StartTime >= start && e.StartTime < end)
                .OrderBy(e => e.StartTime)
                .Select(e => new EventItem
                {
                    EventId = e.EventId,
                    Source = e.Source,
                    Title = e.Title,
                    Description = e.Description,
                    Category = e.Category,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    VenueName = e.VenueName,
                    Address = e.Address,
                    City = e.City,
                    State = e.State,
                    Zip = e.Zip,
                    Latitude = e.Latitude,
                    Longitude = e.Longitude,
                    Url = e.Url,
                    CreatedByUserId = e.CreatedByUserId,
                    IsFavorite = favoriteEventIds.Contains(e.EventId)
                })
                .ToListAsync();

            return results;
        }

        public async Task<List<EventItem>> GetFavoriteEventsForCurrentMonthAsync()
        {
            var all = await GetEventsForCurrentMonthAsync();
            return all.Where(e => e.IsFavorite).ToList();
        }

        // ================= INSERT =================

        public async Task<string> InsertEventAsync(
            string title, string? category, DateTime start, DateTime end,
            string? venue, string? address, string? city, string? state, string? zip,
            string? desc, string? url)
        {
            // Check for duplicates
            var exists = await _context.Events
                .AnyAsync(e => e.Title == title && e.StartTime == start);

            if (exists)
                throw new Exception("An event with same title and time already exists.");

            var (lat, lon) = await TryGeocodeAsync(address, city, state, zip);

            var newId = Guid.NewGuid().ToString();

            var eventItem = new EventItem
            {
                EventId = newId,
                Source = "User",
                Title = title,
                Description = desc,
                Category = category,
                StartTime = start,
                EndTime = end,
                VenueName = venue,
                Address = address,
                City = city,
                State = state,
                Zip = zip,
                Latitude = lat,
                Longitude = lon,
                Url = url,
                CreatedByUserId = _userId
            };

            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            return newId;
        }

        // ================= DELETE =================

        public async Task DeleteEventAsync(string id)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id && e.CreatedByUserId == _userId);

            if (eventItem != null)
            {
                _context.Events.Remove(eventItem);
                await _context.SaveChangesAsync();
            }
        }

        // ================= FAVORITE =================

        public async Task ToggleFavoriteAsync(string eventId)
        {
            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == _userId && f.EventId == eventId);

            if (existing != null)
            {
                _context.Favorites.Remove(existing);
            }
            else
            {
                var favorite = new Favorite
                {
                    UserId = _userId,
                    EventId = eventId
                };
                _context.Favorites.Add(favorite);
            }

            await _context.SaveChangesAsync();
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
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Community-Event-Finder/1.0");

                var response = await client.GetStringAsync(url);
                using var json = JsonDocument.Parse(response);

                if (json.RootElement.GetArrayLength() == 0)
                    return (null, null);

                var item = json.RootElement[0];

                decimal lat = decimal.Parse(item.GetProperty("lat").GetString()!,
                    CultureInfo.InvariantCulture);
                decimal lon = decimal.Parse(item.GetProperty("lon").GetString()!,
                    CultureInfo.InvariantCulture);

                // US bounding box validation
                if (lat < 24 || lat > 50 || lon < -125 || lon > -66)
                    return (null, null);

                return (lat, lon);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}