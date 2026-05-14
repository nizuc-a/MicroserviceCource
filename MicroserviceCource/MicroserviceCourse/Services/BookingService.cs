using MicroserviceCourse.Data;
using MicroserviceCourse.Exceptions;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.Entity;
using MicroserviceCourse.Model.Enum;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceCourse.Services;

public class BookingService(AppDbContext context) : IBookingService
{
    private readonly SemaphoreSlim _bookingLock = new(1,1);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        var value = await context.Events.FirstOrDefaultAsync(x => x.Id == eventId, ct);
        if (value is null)
            throw new KeyNotFoundException($"Event with Id {eventId} not found");

        await _bookingLock.WaitAsync(ct);

        Booking booking;
        try
        {
            var canReserve = value.TryReserveSeats();
            if (!canReserve)
                throw new NoAvailableSeatsException("No available seats for this event");

            booking = new Booking(eventId);
            context.Bookings.Add(booking);

            await context.SaveChangesAsync(ct);
        }
        finally
        {
            _bookingLock.Release();
        }
        
        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
        if (booking == null)
            throw new KeyNotFoundException($"Booking with Id {bookingId} not found");

        return booking;
    }

    public async Task UpdateStatusAsync(Guid bookingId, BookingStatus status, CancellationToken ct = default)
    {
        var booking = await context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
        if (booking == null)
            throw new KeyNotFoundException($"Booking with Id {bookingId} not found");

        booking.Status = status;
        booking.ProcessedAt = DateTime.Now;

        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}