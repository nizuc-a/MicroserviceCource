using UserService.Application.Abstractions.Auth;
using UserService.Application.Abstractions.Repositories;
using UserService.Application.Abstractions.Services;
using UserService.Domain.Enums;
using UserService.Domain.Exceptions;

namespace UserService.Application.Services;

public class AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator) : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid login or password.";

    public async Task<string> LoginAsync(string login, string password, CancellationToken ct = default)
    {
        var user = await userRepository.GetUserByLoginAsync(login, ct);

        if (user is null)
            throw new UserNotFoundException(InvalidCredentialsMessage);

        var passwordHash = passwordHasher.HashPassword(password);

        if (user.PasswordHash != passwordHash)
            throw new UserNotFoundException(InvalidCredentialsMessage);

        return tokenGenerator.GenerateToken(user);
    }

    public async Task RegisterAsync(string login, string password, UserRole role, CancellationToken ct = default)
    {
        var userExist = await userRepository.GetUserByLoginAsync(login, ct);
        if (userExist is not null)
            throw new ArgumentException("Login is already taken.");

        var passwordHash = passwordHasher.HashPassword(password);

        await userRepository.RegisterAsync(login, passwordHash, role, ct);
    }
}