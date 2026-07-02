namespace BookingService.Domain.Exceptions;

public class BookingAlreadyCancelledException(string? message) : Exception(message);
