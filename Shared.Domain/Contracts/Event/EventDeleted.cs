namespace Shared.Domain.Contracts.Event;

public class EventDeleted
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
}