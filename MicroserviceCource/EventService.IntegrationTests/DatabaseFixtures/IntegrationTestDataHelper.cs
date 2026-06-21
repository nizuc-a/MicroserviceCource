using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Infrastructure.DbContext;

namespace EventService.IntegrationTests.DatabaseFixtures;

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

    public static async Task<Event> SeedEventAsync(
        AppDbContext context,
        int totalSeats = 10,
        string title = "Тест")
    {
        var eventEntity = new Event(
            title,
            "Описание",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            totalSeats);

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        return eventEntity;
    }
}
