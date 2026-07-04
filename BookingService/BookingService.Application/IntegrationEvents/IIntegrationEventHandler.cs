using BookingService.Domain.Entities;

namespace BookingService.Application.IntegrationEvents;

public interface IIntegrationEventHandler
{
    Task HandleAsync(InboxMessage message, CancellationToken ct);
}