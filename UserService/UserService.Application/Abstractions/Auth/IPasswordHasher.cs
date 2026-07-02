namespace UserService.Application.Abstractions.Auth;

public interface IPasswordHasher
{
    string HashPassword(string password);
}