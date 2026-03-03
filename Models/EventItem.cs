using System;
using System.ComponentModel.DataAnnotations;

namespace Community_Event_Finder.Models
{
    public class EventItem
    {
        [Key]
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Source { get; set; } = "User";

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }
        public string? Category { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string? VenueName { get; set; }
        public string? Address { get; set; }

        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string? Url { get; set; }

        public string? CreatedByUserId { get; set; }

        public bool IsFavorite { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}