using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Community_Event_Finder.Models
{
    public class Location
    {
        [Key]
        public int LocationId { get; set; }

        [Required]
        [StringLength(200)]
        public string VenueName { get; set; } = "";

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string City { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string State { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string Zip { get; set; } = "";

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        // Navigation property
        [JsonIgnore]
        public ICollection<EventItem> Events { get; set; } = new List<EventItem>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
