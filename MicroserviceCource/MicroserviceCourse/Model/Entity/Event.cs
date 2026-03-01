namespace MicroserviceCourse.Model.Entity;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; }= string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}