using MicroserviceCourse.Model.Entity;

namespace MicroserviceCourse.Interfaces.TaskQueue;

public interface IBookingTaskQueue
{
    void Enqueue(Booking booking);
    bool TryDequeue(out Booking booking);
}