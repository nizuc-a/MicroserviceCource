namespace EventService.Domain.Exceptions;

public class AuthenticationFailedException(string? message) : Exception(message);