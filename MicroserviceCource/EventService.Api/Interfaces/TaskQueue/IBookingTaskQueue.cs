using EventService.Api.Model.Entity;

namespace EventService.Api.Interfaces.TaskQueue;

public interface IBookingTaskQueue
{
    void Enqueue(Booking booking);
    bool TryDequeue(out Booking booking);
    IEnumerable<Booking> GetPending();
}