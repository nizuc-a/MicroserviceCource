using Shared.Domain.Entities;

namespace EventService.Application.IntegrationEvents;

public interface IIntegrationEventHandler
{
    Task HandleAsync(InboxMessage message, CancellationToken ct);
}