namespace BookingService.Domain.Exceptions;

public class PermissionDeniedException(string? message) : Exception(message);