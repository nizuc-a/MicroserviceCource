using System.Collections.Concurrent;
using MicroserviceCourse.Interfaces.TaskQueue;
using MicroserviceCourse.Model.Entity;

namespace MicroserviceCourse.Services.BackgroundServices;

public class InMemoryBookingTaskQueue : IBookingTaskQueue
{
    private readonly ConcurrentQueue<Booking> _queue = new();
    
    public void Enqueue(Booking booking)
    {
        _queue.Enqueue(booking);
    }

    public bool TryDequeue(out Booking booking)
    {
        return _queue.TryDequeue(out booking);
    }
}