using System.Collections.Concurrent;
using System.Text.Json;
using BookingService.Application.Abstractions.Repository;
using BookingService.Application.Abstractions.Services;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;
using Microsoft.Extensions.Options;
using Shared.Domain.Contracts.Booking;
using Shared.Domain.Entities;
using Shared.Domain.Kafka;
using Shared.Domain.Settings;

namespace BookingService.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IOptions<UserSettings> userSettings) : IBookingService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _eventLocks = new();
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _userLocks = new();
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new();

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, int seatCount = 1,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(seatCount, 1);

        var userSemaphore = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

        await userSemaphore.WaitAsync(ct);

        try
        {
            var maxActiveBookingsPerUser = userSettings.Value.MaxActiveBookingsPerUser;

            var activeBookingsCount = await bookingRepository.CountActiveBookingsByUserIdAsync(userId, ct);

            if (activeBookingsCount >= maxActiveBookingsPerUser)
                throw new ActiveBookingLimitExceededException(
                    $"User has reached the maximum limit of {maxActiveBookingsPerUser} active bookings");

            var eventSemaphore = _eventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));

            await eventSemaphore.WaitAsync(ct);

            try
            {
                var booking = new Booking(eventId, userId, seatCount);

                var createdPayload = new BookingCreated
                {
                    BookingId = booking.Id,
                    UserId = userId,
                    EventId = eventId,
                    SeatCount = seatCount,
                    CreatedAt = booking.CreatedAt,
                };

                var outbox = new OutboxMessage
                {
                    Topic = KafkaTopics.Bookings,
                    Key = booking.Id.ToString(),
                    Type = nameof(BookingCreated),
                    Payload = JsonSerializer.Serialize(createdPayload),
                };

                await bookingRepository.CreateBookingAsync(booking, outbox, ct);

                return booking;
            }
            finally
            {
                eventSemaphore.Release();
            }
        }
        finally
        {
            userSemaphore.Release();
        }
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetBookingByIdAsync(bookingId, ct);

        if (booking == null)
            throw new KeyNotFoundException($"Booking with Id {bookingId} not found");

        return booking;
    }

    public async Task<List<Booking>> GetBookingsByUserId(Guid userId, CancellationToken ct = default)
    {
        return await bookingRepository.GetBookingsByUserId(userId, ct);
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var semaphore = _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            var booking = await bookingRepository.GetBookingByIdAsync(bookingId, ct);

            if (booking == null)
                throw new KeyNotFoundException($"Booking with Id {bookingId} not found");

            var isOwner = booking.UserId == userId;
            if (!isAdmin && !isOwner)
                throw new PermissionDeniedException($"User {userId} is not allowed to cancel booking {bookingId}");

            var outbox = GetBookingCancelledOutboxMessage(booking, ct);

            await bookingRepository.CancelBookingAsync(bookingId, outbox, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task CancelBookingsByEventIdAsync(Guid eventId, CancellationToken ct = default)
    {
        var semaphore = _eventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            var bookings = await bookingRepository.GetActiveBookingsByEventIdAsync(eventId, ct);

            foreach (var booking in bookings)
            {
                var outbox = GetBookingCancelledOutboxMessage(booking, ct);

                await bookingRepository.CancelBookingAsync(booking.Id, outbox, ct);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task ConfirmBookingAsync(Guid bookingId, CancellationToken ct = default)
    {
        var semaphore = _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            var booking = await bookingRepository.GetBookingByIdAsync(bookingId, ct);
            if (booking == null || booking.Status != BookingStatus.Pending)
                return;

            booking.Confirm();
            await bookingRepository.SaveChangesAsync(ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task RejectBookingAsync(Guid bookingId, CancellationToken ct = default)
    {
        var semaphore = _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            var booking = await bookingRepository.GetBookingByIdAsync(bookingId, ct);
            if (booking == null || booking.Status != BookingStatus.Pending)
                return;

            booking.Reject();
            await bookingRepository.SaveChangesAsync(ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => bookingRepository.SaveChangesAsync(ct);

    private OutboxMessage GetBookingCancelledOutboxMessage(Booking booking, CancellationToken ct = default)
    {
        var cancelPayload = new BookingCancelled
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            EventId = booking.EventId,
            SeatCount = booking.SeatCount,
        };

        var outbox = new OutboxMessage
        {
            Topic = KafkaTopics.Bookings,
            Key = booking.Id.ToString(),
            Type = nameof(BookingCancelled),
            Payload = JsonSerializer.Serialize(cancelPayload),
        };

        return outbox;
    }
}