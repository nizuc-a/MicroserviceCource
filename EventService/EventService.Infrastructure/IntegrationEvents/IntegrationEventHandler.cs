using System.Text.Json;
using EventService.Application.Abstractions.Services;
using EventService.Application.IntegrationEvents;
using Shared.Domain.Contracts.Booking;
using Shared.Domain.Entities;

namespace EventService.Infrastructure.IntegrationEvents;

public class IntegrationEventHandler(IEventService eventService) : IIntegrationEventHandler
{
    public async Task HandleAsync(InboxMessage message, CancellationToken ct)
    {
        var handleEventDeleted = message.Type switch
        {
            nameof(BookingCancelled) => HandleBookingCancelled(message, ct),
            nameof(BookingCreated) => HandleBookingCreated(message, ct),
            _ => throw new InvalidOperationException($"Unknown event type: {message.Type}")
        };
        
        await handleEventDeleted;
    }

    private async Task HandleBookingCreated(InboxMessage message, CancellationToken ct)
    {
        var booking = JsonSerializer.Deserialize<BookingCreated>(message.Payload) 
                      ?? throw new InvalidOperationException("Invalid BookingCreated payload");
        
        await eventService.BookEvent(booking.EventId, booking.BookingId, booking.UserId, ct);
    }

    private async Task HandleBookingCancelled(InboxMessage message, CancellationToken ct)
    {
        var booking = JsonSerializer.Deserialize<BookingCancelled>(message.Payload)
                     ?? throw new InvalidOperationException("Invalid BookingCancelled payload");

        var entity = await eventService.GetById(booking.EventId, ct);
        
        entity.ReleaseSeats();
        
        await eventService.SaveChangesAsync(ct);
    }
}