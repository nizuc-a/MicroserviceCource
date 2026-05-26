using EventService.Api.Model.Entity;

namespace EventService.Api.Interfaces.Repository;

public interface IBookingRepository
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}