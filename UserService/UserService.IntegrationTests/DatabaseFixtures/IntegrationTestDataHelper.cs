using Shared.Domain.Enums;
using UserService.Domain.Entities;
using UserService.Infrastructure.DbContext;

namespace UserService.IntegrationTests.DatabaseFixtures;

public static class IntegrationTestDataHelper
{
    public static async Task<User> SeedUserAsync(
        AppDbContext context,
        string login = "testuser",
        UserRole role = UserRole.User)
    {
        var user = new User(login, "password_hash", role);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
