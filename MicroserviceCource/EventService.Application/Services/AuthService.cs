using System.Security.Authentication;
using EventService.Application.Abstractions.Auth;
using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Domain.Exceptions;

namespace EventService.Application.Services;

public class AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator) : IAuthService
{
    public async Task<string> LoginAsync(string login, string password, CancellationToken ct = default)
    {
        var user = await userRepository.GetUserByLoginAsync(login, ct);

        if (user is null)
            throw new UserNotFoundException($"User with login '{login}' not found.");
        
        var passwordHash = passwordHasher.HashPassword(password);

        if (user.PasswordHash != passwordHash)
            throw new AuthenticationException("Invalid login or password.");
        
        return tokenGenerator.GenerateToken(user);
    }

    public async Task<string> RegisterAsync(string login, string password, CancellationToken ct = default)
    {
        var userExist = await userRepository.GetUserByLoginAsync(login, ct);
        if(userExist is not null)
            throw new AuthenticationException("Login is busy.");
        
        var passwordHash = passwordHasher.HashPassword(password);
        
        var user = await userRepository.RegisterAsync(login, passwordHash, ct);
        
        return tokenGenerator.GenerateToken(user);
    }
}