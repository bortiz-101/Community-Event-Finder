using System.ComponentModel.DataAnnotations;

public class AddEventDto
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string Category { get; set; }

    [Required]
    public DateTime? Start { get; set; }

    public DateTime? End { get; set; }

    [Required]
    public string Venue { get; set; }

    [Required]
    public string Address { get; set; }

    [Required]
    public string City { get; set; }

    [Required]
    [RegularExpression(@"^[A-Z]{2}$")]
    public string State { get; set; }

    [Required]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Zip must be 5 digits.")]
    public string Zip { get; set; }

    [Required]
    public string Desc { get; set; }

    public string? Url { get; set; }
}
