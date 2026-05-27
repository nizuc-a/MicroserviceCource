using EventService.Domain.Entities;

namespace EventService.Application.Abstractions.TaskQueue;

public interface IBookingTaskQueue
{
    void Enqueue(Booking booking);
    bool TryDequeue(out Booking booking);
    IEnumerable<Booking> GetPending();
}