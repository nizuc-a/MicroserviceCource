using System.ComponentModel.DataAnnotations;

namespace MicroserviceCourse.Model.DTO.Event;

public class UpdateEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; }= string.Empty;

    public DateTime StartAt { get; set; }
    
    public DateTime EndAt { get; set; }
}