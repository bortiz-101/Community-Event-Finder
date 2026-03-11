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

        // Flattened Location properties
        public string? VenueName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Flattened Category property
        public string? Category { get; set; }

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
                VenueName = eventItem.Location?.VenueName,
                Address = eventItem.Location?.Address,
                City = eventItem.Location?.City,
                State = eventItem.Location?.State,
                Zip = eventItem.Location?.Zip,
                Latitude = eventItem.Location?.Latitude,
                Longitude = eventItem.Location?.Longitude,
                Category = eventItem.Category?.Name
            };
        }
    }
}
