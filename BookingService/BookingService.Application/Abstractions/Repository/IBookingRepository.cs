using BookingService.Domain.Entities;
using Shared.Domain.Entities;

namespace BookingService.Application.Abstractions.Repository;

public interface IBookingRepository
{
    Task CreateBookingAsync(Booking booking, OutboxMessage message, CancellationToken ct = default);
    
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);
    
    Task<List<Booking>> GetActiveBookingsByEventIdAsync(Guid eventId, CancellationToken ct = default);
    
    Task<List<Booking>> GetBookingsByUserId(Guid userId, CancellationToken ct = default);

    Task<int> CountActiveBookingsByUserIdAsync(Guid userId, CancellationToken ct = default);
    
    Task CancelBookingAsync(Guid bookingId, OutboxMessage outbox, CancellationToken ct = default);
    
    Task SaveChangesAsync(CancellationToken ct = default);
}