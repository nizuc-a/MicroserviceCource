using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using UserService.Application.Abstractions.Repositories;
using UserService.Domain.Entities;
using UserService.Infrastructure.DbContext;

namespace UserService.Infrastructure.Repository;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken: ct);
    }

    public async Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default)
    {
        return await context.Users.FirstOrDefaultAsync(x => x.Login == login, cancellationToken: ct);
    }

    public async Task<User> RegisterAsync(string login, string passwordHash, UserRole role, CancellationToken ct = default)
    {
        var user = new User(login, passwordHash, role);
        await context.Users.AddAsync(user, ct);
        await context.SaveChangesAsync(ct);
        return user;
    }
}