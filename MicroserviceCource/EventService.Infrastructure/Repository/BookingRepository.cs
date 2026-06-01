using EventService.Application.Abstractions.Repositories;
using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Repository;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task CreateBookingAsync(Booking booking, CancellationToken ct = default)
    {
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await context.Bookings
            .Include(x => x.Event)
            .FirstOrDefaultAsync(x => x.Id == bookingId, ct);
        
        return booking;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>  context.SaveChangesAsync(ct);
}