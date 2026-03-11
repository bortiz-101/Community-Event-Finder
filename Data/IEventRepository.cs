using Community_Event_Finder.Models;

namespace Community_Event_Finder.Data
{
    public interface IEventRepository
    {
        Task<List<EventDto>> GetEventsForCurrentMonthAsync();
        Task<List<EventDto>> GetFavoriteEventsForCurrentMonthAsync();
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
    }
}