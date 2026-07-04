namespace Shared.Domain.Contracts.Booking;

public class BookingCancelled
{
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public Guid BookingId { get; set; }
}