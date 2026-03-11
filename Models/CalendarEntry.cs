using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Community_Event_Finder.Models
{
    public class CalendarEntry
    {
        [Key]
        public int CalendarEntryId { get; set; }

        // Foreign key to User (AspNetUsers.Id)
        [Required]
        public string UserId { get; set; } = "";

        // Foreign key to EventItem (EventId)
        [Required]
        public string EventId { get; set; } = "";

        // Calendar provider type
        [Required]
        public CalendarProvider Provider { get; set; }

        // External calendar event ID (for syncing)
        [StringLength(500)]
        public string? ExternalEventId { get; set; }

        // Navigation properties
        public EventItem? Event { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SyncedAt { get; set; }
    }
}
