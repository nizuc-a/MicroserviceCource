using BookingService.Application.Abstractions.Repository;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;

namespace BookingService.Infrastructure.Repository;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task CreateBookingAsync(Booking booking, OutboxMessage message, CancellationToken ct = default)
    {
        context.Bookings.Add(booking);
        await context.OutboxMessages.AddAsync(message, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await context.Bookings
            .FirstOrDefaultAsync(x => x.Id == bookingId, ct);
        
        return booking;
    }

    public Task<List<Booking>> GetActiveBookingsByEventIdAsync(Guid eventId, CancellationToken ct = default)
    {
        return context.Bookings.Where(x => x.EventId == eventId)
            .Where(x=> x.Status == BookingStatus.Pending || x.Status == BookingStatus.Confirmed)
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetBookingsByUserId(Guid userId, CancellationToken ct = default)
    {
        var bookings = context.Bookings
            .Where(x => x.UserId == userId);
        
        return await bookings.ToListAsync(ct);
    }

    public async Task<int> CountActiveBookingsByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Bookings
            .CountAsync(
                x => x.UserId == userId &&
                     (x.Status == BookingStatus.Pending || x.Status == BookingStatus.Confirmed),
                ct);
    }

    public async Task CancelBookingAsync(Guid bookingId, OutboxMessage outbox, CancellationToken ct = default)
    {
        var booking = context.Bookings
            .FirstOrDefault(x => x.Id == bookingId);
        
        if (booking == null)
            return;
        
        booking.Cancel();
        
        await context.OutboxMessages.AddAsync(outbox, ct);
        
        await SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>  context.SaveChangesAsync(ct);
}