using System.Collections.Concurrent;
using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Domain.Exceptions;

namespace EventService.Application.Services;

public class BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository) : IBookingService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _eventLocks = new();
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new();

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        var semaphore = _eventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(ct);

        try
        {
            var eventEntity = await eventRepository.GetByIdAsync(eventId, ct);
            if (eventEntity == null)
                throw new KeyNotFoundException($"Event with Id {eventId} not found");

            if (eventEntity.StartAt >= DateTime.UtcNow)
                throw new EventExpiredException("Event is already expired");

            if (!eventEntity.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            var booking = new Booking(eventId, userId);

            await bookingRepository.CreateBookingAsync(booking, ct);

            return booking;
        }
        finally
        {
            semaphore.Release();
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

    public async Task CancelBookingAsync(Guid bookingId, CancellationToken ct = default)
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

            await bookingRepository.CancelBookingAsync(bookingId, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => bookingRepository.SaveChangesAsync(ct);
}