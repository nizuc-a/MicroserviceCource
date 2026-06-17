namespace EventService.Domain.Exceptions;

public class UserNotFoundException(string? message) : Exception(message);