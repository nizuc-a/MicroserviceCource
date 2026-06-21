using EventService.Domain.Entities;

namespace EventService.Application.Abstractions.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default);
    
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);
    
    Task<List<Booking>> GetBookingsByUserId(Guid userId, CancellationToken ct = default);
    
    Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default);

    public Task SaveChangesAsync(CancellationToken ct = default);
}