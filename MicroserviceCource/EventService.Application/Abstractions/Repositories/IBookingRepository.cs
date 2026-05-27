using EventService.Domain.Entities;

namespace EventService.Application.Abstractions.Repositories;

public interface IBookingRepository
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}