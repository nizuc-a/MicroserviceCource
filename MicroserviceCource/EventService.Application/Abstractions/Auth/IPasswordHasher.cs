namespace EventService.Application.Abstractions.Auth;

public interface IPasswordHasher
{
    string HashPassword(string password);
}