namespace MicroserviceCourse.Model.Entity;

public class Event
{
    private readonly object _locker = new object();

    public Event(string title, string description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }

    public void Update(string title, string description, DateTime startAt, DateTime endAt, int totalSeats,
        int availableSeats)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = availableSeats;
    }

    public bool TryReserveSeats(int count = 1)
    {
        lock (_locker)
        {
            if (AvailableSeats < count)
                return false;

            AvailableSeats -= count;
            return true;
        }
    }

    public void ReleaseSeats(int count = 1)
    {
        lock (_locker)
        {
            if (AvailableSeats + count > TotalSeats)
                throw new ArgumentOutOfRangeException(nameof(count));
            
            AvailableSeats += count;
        }
    }
}