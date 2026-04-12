using MicroserviceCourse.Model.Enum;

namespace MicroserviceCourse.Model.Entity;

public class Booking
{
    public Booking(Guid eventId)
    {
        EventId = eventId;
    }
    
    public Guid Id { get; set; }
    
    public Guid EventId { get; set; }
    
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? ProcessedAt { get; set; }
}