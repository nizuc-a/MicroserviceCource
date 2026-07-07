using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;

namespace BookingService.Domain.Entities;

public class Booking
{
    public Booking(Guid eventId, Guid userId, int seatCount = 1)
    {
        EventId = eventId;
        UserId = userId;
        SeatCount = seatCount;
    }
    
    public Guid Id { get; set; } =  Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    public Guid EventId { get; set; }

    public int SeatCount { get; set; } = 1;
    
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }
    
    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new BookingAlreadyCancelledException($"Booking with Id {Id} is already cancelled");
        
        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}