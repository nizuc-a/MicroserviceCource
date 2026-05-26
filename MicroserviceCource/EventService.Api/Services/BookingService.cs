using EventService.Api.Interfaces.Repository;
using EventService.Api.Interfaces.Services;
using EventService.Api.Model.Entity;

namespace EventService.Api.Services;

public class BookingService(IBookingRepository bookingRepository) : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        await BookingLock.WaitAsync(ct);
        
        try
        {
            return  await bookingRepository.CreateBookingAsync(eventId, ct);
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