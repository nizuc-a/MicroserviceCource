using System.Collections.Concurrent;
using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;

namespace EventService.Application.Services;

public class BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository) : IBookingService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        var semaphore = _locks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
        
        await semaphore.WaitAsync(ct);
        
        try
        {
            var eventEntity = await eventRepository.GetByIdAsync(eventId, ct);
            if (eventEntity == null)
                throw new KeyNotFoundException($"Event with Id {eventId} not found");
            
            if(eventEntity.StartAt >= DateTime.UtcNow)
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
        await bookingRepository.CancelBookingAsync(bookingId, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>  bookingRepository.SaveChangesAsync(ct);
}