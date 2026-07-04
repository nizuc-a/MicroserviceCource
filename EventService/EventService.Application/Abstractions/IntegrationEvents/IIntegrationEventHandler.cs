using Shared.Domain.Entities;

namespace EventService.Application.Abstractions.IntegrationEvents;

public interface IIntegrationEventHandler
{
    Task HandleAsync(InboxMessage message, CancellationToken ct);
}