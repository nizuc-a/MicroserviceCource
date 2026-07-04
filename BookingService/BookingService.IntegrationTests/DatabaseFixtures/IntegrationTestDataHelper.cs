using System.Text.Json;
using BookingService.Domain.Entities;
using Shared.Domain.Contracts.Booking;
using Shared.Domain.Entities;

namespace BookingService.IntegrationTests.DatabaseFixtures;

public static class IntegrationTestDataHelper
{
    public static OutboxMessage CreateBookingCreatedOutbox(Booking booking)
    {
        var payload = new BookingCreated
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            EventId = booking.EventId,
        };

        return new OutboxMessage
        {
            Topic = "bookings",
            Key = booking.Id.ToString(),
            Type = nameof(BookingCreated),
            Payload = JsonSerializer.Serialize(payload),
        };
    }

    public static OutboxMessage CreateBookingCancelledOutbox(Booking booking)
    {
        var payload = new BookingCancelled
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            EventId = booking.EventId,
        };

        return new OutboxMessage
        {
            Topic = "bookings",
            Key = booking.Id.ToString(),
            Type = nameof(BookingCancelled),
            Payload = JsonSerializer.Serialize(payload),
        };
    }
}
