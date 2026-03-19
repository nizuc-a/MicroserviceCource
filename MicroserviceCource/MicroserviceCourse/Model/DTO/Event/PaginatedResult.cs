namespace MicroserviceCourse.Model.DTO.Event;

public class PaginatedResult
{
    public int AllElementCount { get; set; }
    public int PageNumber { get; set; }
    public Entity.Event[] Events { get; set; }
    public int CurrentPageElementCount { get; set; }
}