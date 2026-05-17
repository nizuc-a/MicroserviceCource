using System.Collections.Concurrent;
using EventService.Api.Interfaces.TaskQueue;
using EventService.Api.Model.Entity;

namespace EventService.Api.Services.BackgroundServices;

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

    public IEnumerable<Booking> GetPending()
    {
        while (TryDequeue(out var booking))
        {
            yield return booking;
        }
    }
}