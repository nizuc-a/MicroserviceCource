using EventService.Domain.Entities;
using EventService.Infrastructure.DbContext;

namespace EventService.IntegrationTests.DatabaseFixtures;

public static class IntegrationTestDataHelper
{
    public static async Task<Event> SeedEventAsync(
        AppDbContext context,
        int totalSeats = 10,
        string title = "Тест")
    {
        var eventEntity = new Event(
            title,
            "Описание",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            totalSeats);

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        return eventEntity;
    }
}
