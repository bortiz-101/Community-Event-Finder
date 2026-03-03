using System.ComponentModel.DataAnnotations;

namespace Community_Event_Finder.Models
{
    public class Favorite
    {
        // Simple surrogate PK (easy for EF + scaffolding)
        public int Id { get; set; }

        // Identity user id (AspNetUsers.Id)
        [Required]
        public string UserId { get; set; } = "";

        // FK to EventItem.EventId (string)
        [Required]
        public string EventId { get; set; } = "";

        // Navigation property
        public EventItem? Event { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
