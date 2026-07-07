using Shared.Domain.Entities;

namespace UserService.Application.Abstractions.IntegrationEvents;

public interface IIntegrationEventHandler
{
    Task HandleAsync(InboxMessage message, CancellationToken ct);
}
