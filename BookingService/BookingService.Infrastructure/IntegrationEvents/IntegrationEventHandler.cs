using System.Text.Json;
using BookingService.Application.Abstractions.Services;
using BookingService.Application.IntegrationEvents;
using Shared.Domain.Contracts.Event;
using Shared.Domain.Entities;

namespace BookingService.Infrastructure.IntegrationEvents;

public class IntegrationEventHandler(IBookingService bookingService) : IIntegrationEventHandler
{
    public async Task HandleAsync(InboxMessage message, CancellationToken ct)
    {
        var handleEventDeleted = message.Type switch
        {
            nameof(EventDeleted) => HandleEventDeleted(message, ct),
            _ => throw new InvalidOperationException($"Unknown event type: {message.Type}")
        };
        
        await handleEventDeleted;
    }

    private async Task HandleEventDeleted(InboxMessage message, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<EventDeleted>(message.Payload)
                     ?? throw new InvalidOperationException("Invalid EventDeleted payload");
        
        await bookingService.CancelBookingsByEventIdAsync(@event.EventId, ct);
    }
}