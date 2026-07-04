using Shared.Domain.Entities;

namespace UserService.Application.IntegrationEvents;

public interface IIntegrationEventHandler
{
    Task HandleAsync(InboxMessage message, CancellationToken ct);
}
