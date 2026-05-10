namespace MicroserviceCourse.Exceptions;

public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException()
    {
        
    }

    public NoAvailableSeatsException(string? message) : base(message)
    {
        
    }
}