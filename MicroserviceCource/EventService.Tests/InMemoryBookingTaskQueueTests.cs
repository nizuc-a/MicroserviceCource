using MicroserviceCourse.Model.Entity;
using MicroserviceCourse.Services.BackgroundServices;

namespace EventService.Tests;

public class InMemoryBookingTaskQueueTests
{
    private readonly InMemoryBookingTaskQueue _bookingTaskQueue;
    
    public InMemoryBookingTaskQueueTests()
    {
        _bookingTaskQueue = new InMemoryBookingTaskQueue();
    }
    
    [Fact]
    public void EnqueueAndDequeueBooking_Correct()
    {
        var booking = new Booking(Guid.NewGuid());
        
        _bookingTaskQueue.Enqueue(booking);

        var canDequeue = _bookingTaskQueue.TryDequeue(out var bookingResult);
        
        Assert.True(canDequeue);
        Assert.Equal(booking, bookingResult);
    }
    
    [Fact]
    public void TryDequeue_Empty_Correct()
    {
        var canDequeue = _bookingTaskQueue.TryDequeue(out var bookingResult);
        
        Assert.False(canDequeue);
    }
}