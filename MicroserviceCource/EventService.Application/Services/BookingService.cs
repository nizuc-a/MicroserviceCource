using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;

namespace EventService.Application.Services;

public class BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository) : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        await BookingLock.WaitAsync(ct);
        
        try
        {
            var eventEntity = await eventRepository.GetByIdAsync(eventId, ct);
            if (eventEntity == null)
                throw new KeyNotFoundException($"Event with Id {eventId} not found");
            
            if (!eventEntity.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
            
            var booking = new Booking(eventId);
            
            await bookingRepository.CreateBookingAsync(booking, ct);

            return booking;
        }
        finally
        {
            BookingLock.Release();
        }
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetBookingByIdAsync(bookingId, ct);
        
        if (booking == null)
            throw new KeyNotFoundException($"Booking with Id {bookingId} not found");
        
        return booking;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>  bookingRepository.SaveChangesAsync(ct);
}