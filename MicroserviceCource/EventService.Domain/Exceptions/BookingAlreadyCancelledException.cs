namespace EventService.Domain.Exceptions;

public class BookingAlreadyCancelledException(string? message) : Exception(message);
