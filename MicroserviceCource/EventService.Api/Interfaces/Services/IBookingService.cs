using EventService.Api.Model.Entity;
using EventService.Api.Model.Enum;

namespace EventService.Api.Interfaces.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    public Task SaveChangesAsync(CancellationToken ct = default);
}