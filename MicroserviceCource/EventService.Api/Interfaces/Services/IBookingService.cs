using EventService.Domain.Entities;

namespace EventService.Api.Interfaces.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    public Task SaveChangesAsync(CancellationToken ct = default);
}