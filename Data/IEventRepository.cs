using Community_Event_Finder.Models;

namespace Community_Event_Finder.Data
{
    public interface IEventRepository
    {
        Task<List<EventDto>> GetEventsForCurrentMonthAsync();
        Task<List<EventDto>> GetFavoriteEventsForCurrentMonthAsync();
        Task<List<EventDto>> GetEventsByMonthAsync(int year, int month);
        Task<EventDto?> GetEventByIdAsync(string eventId);
        Task ToggleFavoriteAsync(string eventId);

        Task<string> InsertEventAsync(
            string title,
            string? category,
            DateTime start,
            DateTime end,
            string? venue,
            string? address,
            string? city,
            string? state,
            string? zip,
            string? desc,
            string? url);

        Task DeleteEventAsync(string id);

        // ============= EXTERNAL EVENT DEDUPLICATION =============

        /// <summary>
        /// Retrieves an event by its external provider ID.
        /// </summary>
        Task<EventItem?> GetEventByExternalIdAsync(string externalEventId, EventSourceType sourceType);

        /// <summary>
        /// Retrieves an event by secondary deduplication key.
        /// Falls back to: Title + StartTime + VenueName
        /// </summary>
        Task<EventItem?> GetEventBySecondaryKeyAsync(string title, DateTime startTime, string? venueName);

        /// <summary>
        /// Batch upsert (insert or update) multiple external events.
        /// Uses deduplication logic:
        /// - Primary: (ExternalEventId, SourceType)
        /// - Secondary fallback: (Title, StartTime, VenueName)
        /// </summary>
        Task<List<string>> UpsertManyAsync(List<EventItem> events);
    }
}