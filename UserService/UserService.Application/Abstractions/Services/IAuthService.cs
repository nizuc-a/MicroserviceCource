using Shared.Domain.Enums;

namespace UserService.Application.Abstractions.Services;

public interface IAuthService
{
    Task<string> LoginAsync(string login, string password, CancellationToken ct = default);
    
    Task RegisterAsync(string login, string password, UserRole role, CancellationToken ct = default);
}