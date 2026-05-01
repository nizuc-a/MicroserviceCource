using MicroserviceCourse.Model.Entity;
using MicroserviceCourse.Model.Enum;

namespace MicroserviceCourse.Interfaces.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    public Task UpdateStatusAsync(Guid bookingId,BookingStatus status, CancellationToken ct = default);
}