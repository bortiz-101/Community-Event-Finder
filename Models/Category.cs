using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Community_Event_Finder.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation property
        [JsonIgnore]
        public ICollection<EventItem> Events { get; set; } = new List<EventItem>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
