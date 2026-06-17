namespace EventService.Domain.Exceptions;

public class PermissionDeniedException(string? message) : Exception(message);