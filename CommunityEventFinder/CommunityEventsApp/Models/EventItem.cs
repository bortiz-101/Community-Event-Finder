using System;

namespace CommunityEventsApp.Models
{
    public class EventItem
    {
        public string EventId { get; set; }
        public string Source { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string VenueName { get; set; }
        public string Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Url { get; set; }
    }
}
