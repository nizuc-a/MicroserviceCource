using EventService.Application.Abstractions.Repositories;
using EventService.Domain.Entities;
using EventService.Infrastructure.DbContext;
using EventService.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Domain.Entities;

namespace EventService.UnitTests;

public class EventRepositoryCacheTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CacheTestDb_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_DoesNotQueryDatabase()
    {
        await using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var cached = new Event("cached", "desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10)
        {
            Id = eventId
        };

        // Entity only in cache — not in DB. Cache hit must return without DB.
        var cache = new Mock<ICacheRepository>();
        cache.Setup(c => c.TryGetValueAsync<Event>($"event:{eventId}"))
            .ReturnsAsync((true, cached));

        var repository = new EventRepository(context, cache.Object, TestRedisSettings.Default);

        var result = await repository.GetByIdAsync(eventId);

        Assert.Same(cached, result);
        cache.Verify(c => c.AddAsync(It.IsAny<string>(), It.IsAny<Event>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_LoadsFromDatabaseAndStoresInCache()
    {
        await using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var entity = new Event("from-db", "desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10)
        {
            Id = eventId
        };
        context.Events.Add(entity);
        await context.SaveChangesAsync();

        var cache = new Mock<ICacheRepository>();
        cache.Setup(c => c.TryGetValueAsync<Event>($"event:{eventId}"))
            .ReturnsAsync((false, null));
        cache.Setup(c => c.AddAsync($"event:{eventId}", It.IsAny<Event>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        var repository = new EventRepository(context, cache.Object, TestRedisSettings.Default);

        var result = await repository.GetByIdAsync(eventId);

        Assert.NotNull(result);
        Assert.Equal(eventId, result!.Id);
        cache.Verify(
            c => c.AddAsync(
                $"event:{eventId}",
                It.Is<Event>(e => e.Id == eventId),
                TimeSpan.FromMinutes(1)),
            Times.Once);
    }

    [Fact]
    public async Task GetTop_CacheHit_DoesNotQueryDatabase()
    {
        await using var context = CreateContext();
        var cachedTop = new[]
        {
            new Event("top", "desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10)
        };

        var cache = new Mock<ICacheRepository>();
        cache.Setup(c => c.TryGetValueAsync<Event[]>("events:top10"))
            .ReturnsAsync((true, cachedTop));

        var repository = new EventRepository(context, cache.Object, TestRedisSettings.Default);

        var result = await repository.GetTop(10);

        Assert.Same(cachedTop, result);
        cache.Verify(c => c.AddAsync(It.IsAny<string>(), It.IsAny<Event[]>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetTop_CacheMiss_LoadsFromDatabaseAndStoresInCache()
    {
        await using var context = CreateContext();
        var popular = new Event("popular", "desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10)
        {
            AvailableSeats = 2
        };
        var unpopular = new Event("unpopular", "desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        context.Events.AddRange(popular, unpopular);
        await context.SaveChangesAsync();

        var cache = new Mock<ICacheRepository>();
        cache.Setup(c => c.TryGetValueAsync<Event[]>("events:top10"))
            .ReturnsAsync((false, null));
        cache.Setup(c => c.AddAsync("events:top10", It.IsAny<Event[]>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        var repository = new EventRepository(context, cache.Object, TestRedisSettings.Default);

        var result = await repository.GetTop(10);

        Assert.NotEmpty(result);
        Assert.Equal(popular.Id, result[0].Id);
        cache.Verify(
            c => c.AddAsync(
                "events:top10",
                It.Is<Event[]>(arr => arr.Length > 0),
                TimeSpan.FromMinutes(5)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateEvent_InvalidatesCacheAfterSave()
    {
        await using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var entity = new Event("old", "desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10)
        {
            Id = eventId
        };
        context.Events.Add(entity);
        await context.SaveChangesAsync();

        var cache = new Mock<ICacheRepository>();
        cache.Setup(c => c.RemoveAsync($"event:{eventId}")).Returns(Task.CompletedTask);

        var repository = new EventRepository(context, cache.Object, TestRedisSettings.Default);
        entity.Update("new", "desc", entity.StartAt, entity.EndAt, 10, 10);

        await repository.UpdateEvent(entity);

        cache.Verify(c => c.RemoveAsync($"event:{eventId}"), Times.Once);
    }

    [Fact]
    public async Task DeleteEventByIdAsync_InvalidatesCacheAfterSave()
    {
        await using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var entity = new Event("to-delete", "desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10)
        {
            Id = eventId
        };
        context.Events.Add(entity);
        await context.SaveChangesAsync();

        var cache = new Mock<ICacheRepository>();
        cache.Setup(c => c.RemoveAsync($"event:{eventId}")).Returns(Task.CompletedTask);

        var repository = new EventRepository(context, cache.Object, TestRedisSettings.Default);
        var outbox = new OutboxMessage
        {
            Topic = "events",
            Key = eventId.ToString(),
            Type = "EventDeleted",
            Payload = "{}"
        };

        await repository.DeleteEventByIdAsync(eventId, outbox);

        cache.Verify(c => c.RemoveAsync($"event:{eventId}"), Times.Once);
        Assert.Null(await context.Events.FirstOrDefaultAsync(e => e.Id == eventId));
    }
}
