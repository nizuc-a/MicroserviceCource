namespace Shared.Domain.Contracts.Booking;

public class BookingConfirmed
{
    public Guid BookingId { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
}
