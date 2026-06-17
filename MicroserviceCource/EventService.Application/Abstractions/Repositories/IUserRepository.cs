using EventService.Domain.Entities;

namespace EventService.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    
    Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default);
    
    Task<User> RegisterAsync(string login, string password, CancellationToken ct = default);
}