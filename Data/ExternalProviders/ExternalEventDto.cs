namespace Community_Event_Finder.Data.ExternalProviders
{
    // External event data transfer object for normalizing events from different providers
    public class ExternalEventDto
    {
        public string? ExternalId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? VenueName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Category { get; set; }
        public string? Url { get; set; }
        public string? Source { get; set; } // Provider name
    }
}
