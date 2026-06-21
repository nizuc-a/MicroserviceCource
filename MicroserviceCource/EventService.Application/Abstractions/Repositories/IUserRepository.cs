using EventService.Domain.Entities;
using EventService.Domain.Enums;

namespace EventService.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    
    Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default);
    
    Task<User> RegisterAsync(string login, string password, UserRole role, CancellationToken ct = default);
}