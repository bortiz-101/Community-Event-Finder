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

        public async Task<List<EventDto>> GetEventsForCurrentMonthAsync()
        {
            // Events in 1 month starting from today
            var start = DateTime.Today;
            var end = start.AddMonths(12);

            var favoriteEventIds = await _context.Favorites
                .Where(f => f.UserId == _userId)
                .Select(f => f.EventId)
                .ToListAsync();

            var results = await _context.Events
                .Where(e => e.StartTime >= start && e.StartTime < end)
                .OrderBy(e => e.StartTime)
                .Include(e => e.Location)
                .Include(e => e.Category)
                .ToListAsync();

            // Set IsFavorite flag and convert to DTO
            var dtos = new List<EventDto>();
            foreach (var evt in results)
            {
                evt.IsFavorite = favoriteEventIds.Contains(evt.EventId);
                dtos.Add(EventDto.FromEventItem(evt));
            }

            return dtos;
        }

        public async Task<List<EventDto>> GetFavoriteEventsForCurrentMonthAsync()
        {
            var all = await GetEventsForCurrentMonthAsync();
            return all.Where(e => e.IsFavorite).ToList();
        }

        public async Task<List<EventDto>> GetEventsByMonthAsync(int year, int month)
        {
            // Validate month
            if (month < 1 || month > 12)
                throw new ArgumentException("Month must be between 1 and 12.", nameof(month));

            // Calculate start and end dates for the given month
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            var favoriteEventIds = await _context.Favorites
                .Where(f => f.UserId == _userId)
                .Select(f => f.EventId)
                .ToListAsync();

            var results = await _context.Events
                .Where(e => e.StartTime >= start && e.StartTime < end)
                .OrderBy(e => e.StartTime)
                .Include(e => e.Location)
                .Include(e => e.Category)
                .ToListAsync();

            // Set IsFavorite flag and convert to DTO
            var dtos = new List<EventDto>();
            foreach (var evt in results)
            {
                evt.IsFavorite = favoriteEventIds.Contains(evt.EventId);
                dtos.Add(EventDto.FromEventItem(evt));
            }

            return dtos;
        }

        public async Task<EventDto?> GetEventByIdAsync(string eventId)
        {
            var evt = await _context.Events
                .AsNoTracking()
                .Where(e => e.EventId == eventId)
                .Include(e => e.Location)
                .Include(e => e.Category)
                .FirstOrDefaultAsync();

            if (evt == null)
                return null;

            var favoriteEventIds = await _context.Favorites
                .Where(f => f.UserId == _userId)
                .Select(f => f.EventId)
                .ToListAsync();

            evt.IsFavorite = favoriteEventIds.Contains(evt.EventId);
            return EventDto.FromEventItem(evt);
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

            var newId = Guid.NewGuid().ToString();

            // Create or get Location
            Location? location = null;
            if (!string.IsNullOrWhiteSpace(venue) || !string.IsNullOrWhiteSpace(address))
            {
                var (lat, lon) = await TryGeocodeAsync(address ?? "", city ?? "", state ?? "", zip ?? "");

                location = new Location
                {
                    VenueName = venue ?? "",
                    Address = address ?? "",
                    City = city ?? "",
                    State = state ?? "",
                    Zip = zip ?? "",
                    Latitude = lat,
                    Longitude = lon
                };

                _context.Locations.Add(location);
                await _context.SaveChangesAsync();
            }

            // Get or create Category
            Category? categoryObj = null;
            if (!string.IsNullOrWhiteSpace(category))
            {
                categoryObj = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == category);

                if (categoryObj == null)
                {
                    categoryObj = new Category { Name = category };
                    _context.Categories.Add(categoryObj);
                    await _context.SaveChangesAsync();
                }
            }

            var eventItem = new EventItem
            {
                EventId = newId,
                Source = "User",
                Title = title,
                Description = desc,
                CategoryId = categoryObj?.CategoryId,
                LocationId = location?.LocationId,
                StartTime = start,
                EndTime = end,
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

        // ============= EXTERNAL EVENT DEDUPLICATION =============

        /// <summary>
        /// Retrieves an event by its external provider ID.
        /// Primary deduplication key: (ExternalEventId, ExternalEventSourceType)
        /// </summary>
        public async Task<EventItem?> GetEventByExternalIdAsync(string externalEventId, EventSourceType sourceType)
        {
            if (string.IsNullOrWhiteSpace(externalEventId))
                return null;

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.ExternalEventId == externalEventId &&
                    e.ExternalEventSourceType == sourceType);

            return eventItem;
        }

        /// <summary>
        /// Retrieves an event by secondary deduplication key.
        /// Secondary key: (Title, StartTime, VenueName)
        /// Used when no ExternalEventId is available.
        /// </summary>
        public async Task<EventItem?> GetEventBySecondaryKeyAsync(string title, DateTime startTime, string? venueName)
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            var query = _context.Events
                .Where(e =>
                    e.Title == title &&
                    e.StartTime == startTime);

            // If venue name is provided, check against location
            if (!string.IsNullOrWhiteSpace(venueName))
            {
                query = query.Where(e =>
                    e.Location != null &&
                    e.Location.VenueName == venueName);
            }

            var eventItem = await query.FirstOrDefaultAsync();
            return eventItem;
        }

        /// <summary>
        /// Batch upsert (insert or update) multiple external events.
        /// Deduplication strategy:
        /// 1. Primary: Check by (ExternalEventId, SourceType)
        /// 2. Secondary fallback: Check by (Title, StartTime, VenueName)
        /// 3. If found: Update existing record
        /// 4. If not found: Insert new record
        /// Returns list of EventIds (both new and updated)
        /// </summary>
        public async Task<List<string>> UpsertManyAsync(List<EventItem> events)
        {
            var result = new List<string>();

            if (events == null || events.Count == 0)
                return result;

            foreach (var incomingEvent in events)
            {
                EventItem? existing = null;

                // Try primary deduplication key: (ExternalEventId, SourceType)
                if (!string.IsNullOrWhiteSpace(incomingEvent.ExternalEventId) &&
                    incomingEvent.ExternalEventSourceType.HasValue)
                {
                    existing = await GetEventByExternalIdAsync(
                        incomingEvent.ExternalEventId,
                        incomingEvent.ExternalEventSourceType.Value);
                }

                // Try secondary deduplication key if primary didn't match
                if (existing == null && !string.IsNullOrWhiteSpace(incomingEvent.Title))
                {
                    var venueName = incomingEvent.Location?.VenueName;
                    existing = await GetEventBySecondaryKeyAsync(
                        incomingEvent.Title,
                        incomingEvent.StartTime,
                        venueName);
                }

                if (existing != null)
                {
                    // Update existing event
                    existing.Title = incomingEvent.Title;
                    existing.Description = incomingEvent.Description;
                    existing.StartTime = incomingEvent.StartTime;
                    existing.EndTime = incomingEvent.EndTime;
                    existing.Url = incomingEvent.Url;
                    existing.LocationId = incomingEvent.LocationId;
                    existing.CategoryId = incomingEvent.CategoryId;
                    existing.ExternalEventId = incomingEvent.ExternalEventId;
                    existing.ExternalEventSourceType = incomingEvent.ExternalEventSourceType;

                    _context.Events.Update(existing);
                    result.Add(existing.EventId);
                }
                else
                {
                    // Insert new event
                    if (string.IsNullOrWhiteSpace(incomingEvent.EventId))
                        incomingEvent.EventId = Guid.NewGuid().ToString();

                    _context.Events.Add(incomingEvent);
                    result.Add(incomingEvent.EventId);
                }
            }

            await _context.SaveChangesAsync();
            return result;
        }
    }
}