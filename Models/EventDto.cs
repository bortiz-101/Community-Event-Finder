namespace Community_Event_Finder.Models
{
    public class EventDto
    {
        public string EventId { get; set; } = "";
        public string Source { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Url { get; set; }
        public bool IsFavorite { get; set; }

        // Nested DTOs instead of flattened properties
        public LocationDto? Location { get; set; }
        public CategoryDto? Category { get; set; }

        public static EventDto FromEventItem(EventItem eventItem)
        {
            return new EventDto
            {
                EventId = eventItem.EventId,
                Source = eventItem.Source,
                Title = eventItem.Title,
                Description = eventItem.Description,
                StartTime = eventItem.StartTime,
                EndTime = eventItem.EndTime,
                Url = eventItem.Url,
                IsFavorite = eventItem.IsFavorite,
                Location = eventItem.Location != null ? LocationDto.FromLocation(eventItem.Location) : null,
                Category = eventItem.Category != null ? CategoryDto.FromCategory(eventItem.Category) : null
            };
        }
    }
}
