using System.ComponentModel.DataAnnotations;

namespace MicroserviceCourse.Model.DTO.Event;

public class AddEventDto
{
    [Required]
    [StringLength(256, MinimumLength = 3,  ErrorMessage = "Name must be between 3 and 256 characters")]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    [Required]
    public DateTime StartAt { get; set; }
    
    [Required]
    public DateTime EndAt { get; set; }
}