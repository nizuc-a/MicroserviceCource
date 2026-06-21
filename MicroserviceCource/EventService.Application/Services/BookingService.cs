using System.Collections.Concurrent;
using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Domain.Exceptions;

namespace EventService.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    IUserRepository userRepository) : IBookingService
{
    private const int MaxActiveBookingsPerUser = 10;

    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _eventLocks = new();
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _userLocks = new();
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new();

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        var userSemaphore = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

        await userSemaphore.WaitAsync(ct);

        try
        {
            var user = await userRepository.GetUserByIdAsync(userId, ct);
            if (user == null)
                throw new UserNotFoundException($"User with Id {userId} not found");

            var activeBookingsCount = await bookingRepository.CountActiveBookingsByUserIdAsync(userId, ct);
            if (activeBookingsCount >= MaxActiveBookingsPerUser)
                throw new ActiveBookingLimitExceededException(
                    $"User has reached the maximum limit of {MaxActiveBookingsPerUser} active bookings");

            var eventSemaphore = _eventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));

            await eventSemaphore.WaitAsync(ct);

            try
            {
                var eventEntity = await eventRepository.GetByIdAsync(eventId, ct);
                if (eventEntity == null)
                    throw new KeyNotFoundException($"Event with Id {eventId} not found");

                if (eventEntity.StartAt <= DateTime.UtcNow)
                    throw new EventExpiredException("Event has already started");

                if (!eventEntity.TryReserveSeats())
                    throw new NoAvailableSeatsException("No available seats for this event");

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

            if (booking.Status == BookingStatus.Cancelled)
                throw new BookingAlreadyCancelledException($"Booking with Id {bookingId} is already cancelled");

            var isOwner = booking.UserId == userId;
            if (!isAdmin && !isOwner)
                throw new PermissionDeniedException($"User {userId} is not allowed to cancel booking {bookingId}");

            await bookingRepository.CancelBookingAsync(bookingId, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => bookingRepository.SaveChangesAsync(ct);
}