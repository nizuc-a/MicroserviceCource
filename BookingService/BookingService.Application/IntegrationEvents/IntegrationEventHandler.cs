using System.Text.Json;
using BookingService.Application.Abstractions.IntegrationEvents;
using BookingService.Application.Abstractions.Services;
using Shared.Domain.Contracts.Booking;
using Shared.Domain.Contracts.Event;
using Shared.Domain.Entities;

namespace BookingService.Application.IntegrationEvents;

public class IntegrationEventHandler(IBookingService bookingService) : IIntegrationEventHandler
{
    public async Task HandleAsync(InboxMessage message, CancellationToken ct)
    {
        switch (message.Type)
        {
            case nameof(EventDeleted):
                await HandleEventDeleted(message, ct);
                break;
            case nameof(BookingConfirmed):
                await HandleBookingConfirmed(message, ct);
                break;
            case nameof(BookingRejected):
                await HandleBookingRejected(message, ct);
                break;
            case nameof(BookingCreated):
            case nameof(BookingCancelled):
                break;
            default:
                throw new InvalidOperationException($"Unknown event type: {message.Type}");
        }
    }

    private async Task HandleEventDeleted(InboxMessage message, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<EventDeleted>(message.Payload)
                     ?? throw new InvalidOperationException("Invalid EventDeleted payload");

        await bookingService.CancelBookingsByEventIdAsync(@event.EventId, ct);
    }

    private async Task HandleBookingConfirmed(InboxMessage message, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<BookingConfirmed>(message.Payload)
                      ?? throw new InvalidOperationException("Invalid BookingConfirmed payload");

        await bookingService.ConfirmBookingAsync(payload.BookingId, ct);
    }

    private async Task HandleBookingRejected(InboxMessage message, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<BookingRejected>(message.Payload)
                      ?? throw new InvalidOperationException("Invalid BookingRejected payload");

        await bookingService.RejectBookingAsync(payload.BookingId, ct);
    }
}
