namespace Shared.Domain.Contracts.Booking;

public class BookingCancelled
{
    public Guid BookingId { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public int SeatCount { get; set; } = 1;
}
