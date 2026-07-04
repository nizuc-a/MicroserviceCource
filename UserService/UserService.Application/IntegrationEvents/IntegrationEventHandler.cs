using System.Text.Json;
using Shared.Domain.Contracts.Booking;
using Shared.Domain.Entities;
using UserService.Application.Abstractions.IntegrationEvents;
using UserService.Application.Abstractions.Repositories;

namespace UserService.Application.IntegrationEvents;

public class IntegrationEventHandler(IUserRepository userRepository) : IIntegrationEventHandler
{
    public async Task HandleAsync(InboxMessage message, CancellationToken ct)
    {
        switch (message.Type)
        {
            case nameof(BookingCreated):
                await HandleBookingCreated(message, ct);
                break;
            default:
                throw new InvalidOperationException($"Unknown event type: {message.Type}");
        }
    }

    private async Task HandleBookingCreated(InboxMessage message, CancellationToken ct)
    {
        var booking = JsonSerializer.Deserialize<BookingCreated>(message.Payload)
                      ?? throw new InvalidOperationException("Invalid BookingCreated payload");

        await userRepository.AddBookingAsync(booking.UserId, booking.BookingId, ct);
    }
}
