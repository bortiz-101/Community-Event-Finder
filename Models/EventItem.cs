using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Community_Event_Finder.Models
{
    public class EventItem
    {
        [Key]
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string Source { get; set; } = "User";

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        // Foreign key to Category
        public int? CategoryId { get; set; }

        // Foreign key to Location
        public int? LocationId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string? Url { get; set; }

        public string? CreatedByUserId { get; set; }

        public bool IsFavorite { get; set; }

        // Navigation properties
        public Category? Category { get; set; }

        public Location? Location { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}