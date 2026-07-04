using Shared.Domain.Entities;

namespace BookingService.Application.Abstractions.IntegrationEvents;

public interface IIntegrationEventHandler
{
    Task HandleAsync(InboxMessage message, CancellationToken ct);
}