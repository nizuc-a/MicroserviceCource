using EventService.Application.Abstractions.Repositories;
using EventService.Domain.Entities;
using EventService.Domain.Settings;
using EventService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Domain.Entities;

namespace EventService.Infrastructure.Repository;

public class EventRepository(
    AppDbContext context,
    ICacheRepository cacheRepository,
    IOptions<RedisSettings> redisSettings) : IEventRepository
{
    private readonly RedisSettings _redisSettings = redisSettings.Value;

    public async Task<(Event[], int)> GetAll(string? title = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = context.Events
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);

        var allElementCount = await query.CountAsync(ct);

        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        var events = await query.ToArrayAsync(ct);

        return new(events, allElementCount);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var key = CacheKeys.EventById(id);

        var (found, cached) = await cacheRepository.TryGetValueAsync<Event>(key);
        if (found)
            return cached;

        var entity = await context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is not null)
            await cacheRepository.AddAsync(key, entity, TimeSpan.FromMinutes(_redisSettings.EventTtlMinutes));

        return entity;
    }

    public Task<Event?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
        => context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task InvalidateCacheAsync(Guid id)
        => cacheRepository.RemoveAsync(CacheKeys.EventById(id));

    public async Task<Event[]> GetTop(int count, CancellationToken ct = default)
    {
        var key = CacheKeys.TopEvents(count);

        var (found, cached) = await cacheRepository.TryGetValueAsync<Event[]>(key);
        if (found)
            return cached ?? [];

        var results = await context.Events
            .OrderByDescending(e => (e.TotalSeats - e.AvailableSeats) / (double)e.TotalSeats)
            .Take(count)
            .ToArrayAsync(ct);

        if (results.Length > 0)
            await cacheRepository.AddAsync(key, results, TimeSpan.FromMinutes(_redisSettings.TopTtlMinutes));

        return results;
    }

    public async Task AddEventAsync(Event data, CancellationToken ct = default)
    {
        await context.Events.AddAsync(data, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateEvent(Event data, CancellationToken ct = default)
    {
        var key = CacheKeys.EventById(data.Id);

        context.Events.Update(data);
        await context.SaveChangesAsync(ct);

        await cacheRepository.RemoveAsync(key);
    }

    public async Task DeleteEventByIdAsync(Guid id, OutboxMessage message, CancellationToken ct = default)
    {
        var key = CacheKeys.EventById(id);

        var entity = await GetTrackedByIdAsync(id, ct);
        if (entity == null)
            return;

        context.Events.Remove(entity);
        await context.OutboxMessages.AddAsync(message, ct);
        await context.SaveChangesAsync(ct);

        await cacheRepository.RemoveAsync(key);
    }

    public async Task AddOutboxMessageAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await context.OutboxMessages.AddAsync(message, ct);
        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
