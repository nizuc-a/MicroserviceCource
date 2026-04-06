using MicroserviceCourse.Data;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.Entity;
using MicroserviceCourse.Model.Enum;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceCourse.Services;

public class BookingService(AppDbContext context) : IBookingService
{
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        var hasEvent = await context.Events.AnyAsync(x => x.Id == eventId, ct);
        if (!hasEvent)
            throw new KeyNotFoundException($"Event with Id {eventId} not found");
        
        var booking = new Booking(eventId);
        
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(ct);
        
        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
        if(booking == null)
            throw new KeyNotFoundException($"Booking with Id {bookingId} not found");
        
        return booking;
    }

    public async Task UpdateStatusAsync(Guid bookingId, BookingStatus status, CancellationToken ct = default)
    {
        var booking = await context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
        if(booking == null)
            throw new KeyNotFoundException($"Booking with Id {bookingId} not found");
        
        booking.Status = status;
        booking.ProcessedAt = DateTime.Now;
        
        await context.SaveChangesAsync(ct);
    }
}