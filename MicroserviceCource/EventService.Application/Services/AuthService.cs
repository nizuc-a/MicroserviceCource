using EventService.Application.Abstractions.Auth;
using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Domain.Enums;
using EventService.Domain.Exceptions;

namespace EventService.Application.Services;

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