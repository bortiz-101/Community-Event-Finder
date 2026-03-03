using System.ComponentModel.DataAnnotations;

namespace Community_Event_Finder.Models
{
    public class AddEventDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        public string? Category { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string? VenueName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }

        public string? Description { get; set; }
        public string? Url { get; set; }
    }
}
