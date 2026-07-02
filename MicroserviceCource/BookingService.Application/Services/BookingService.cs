using System.Collections.Concurrent;
using BookingService.Application.Abstractions.Repository;
using BookingService.Application.Abstractions.Services;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;
using BookingService.Domain.Options;
using EventService.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace BookingService.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IOptions<UserSettings> userSettings) : IBookingService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _eventLocks = new();
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _userLocks = new();
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new();

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
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
                //TODO: Слушать отмену брони 
                
                // if (eventEntity.StartAt <= DateTime.UtcNow)
                //     throw new EventExpiredException("Event has already started");
                //
                // if (!eventEntity.TryReserveSeats())
                //     throw new NoAvailableSeatsException("No available seats for this event");

                var booking = new Booking(eventId, userId);

                await bookingRepository.CreateBookingAsync(booking, ct);

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

            await bookingRepository.CancelBookingAsync(bookingId, ct);
            
            //TODO: Послать сигнал об отмене брони
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => bookingRepository.SaveChangesAsync(ct);
}