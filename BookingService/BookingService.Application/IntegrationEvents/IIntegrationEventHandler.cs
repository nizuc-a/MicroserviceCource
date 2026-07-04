using BookingService.Domain.Entities;
using Shared.Domain.Entities;

namespace BookingService.Application.IntegrationEvents;

public interface IIntegrationEventHandler
{
    Task HandleAsync(InboxMessage message, CancellationToken ct);
}