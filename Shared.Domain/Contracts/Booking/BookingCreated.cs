namespace Shared.Domain.Contracts.Booking;

public class BookingCreated
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public Guid BookingId { get; set; }
}