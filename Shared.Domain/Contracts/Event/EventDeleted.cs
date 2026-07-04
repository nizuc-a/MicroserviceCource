namespace Shared.Domain.Contracts.Event;

public record EventDeleted
{
    public Guid EventId { get; init; }

    public EventDeleted(Guid eventId)
    {
        EventId = eventId;
    }
}