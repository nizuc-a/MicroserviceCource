using EventService.Domain.Entities;

namespace EventService.Api.Interfaces.TaskQueue;

public interface IBookingTaskQueue
{
    void Enqueue(Booking booking);
    bool TryDequeue(out Booking booking);
    IEnumerable<Booking> GetPending();
}