using EventService.Api.Data;
using EventService.Api.Exceptions;
using EventService.Api.Interfaces.Repository;
using EventService.Api.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventService.Api.Repository;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        var value = await context.Events.FirstOrDefaultAsync(x => x.Id == eventId, ct);
        if (value is null)
            throw new KeyNotFoundException($"Event with Id {eventId} not found");
            
        var canReserve = value.TryReserveSeats();
        if (!canReserve)
            throw new NoAvailableSeatsException("No available seats for this event");

        var booking = new Booking(eventId);
        context.Bookings.Add(booking);

        await context.SaveChangesAsync(ct);
            
        return booking;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await context.Bookings
            .Include(x => x.Event)
            .FirstOrDefaultAsync(x => x.Id == bookingId, ct);
        
        return booking;
    }
}