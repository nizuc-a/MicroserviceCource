namespace EventService.Domain.Exceptions;

public class EventExpiredException(string? message) : Exception(message);