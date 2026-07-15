using EventService.Application.Abstractions.Repositories;

namespace EventService.IntegrationTests;

internal sealed class NullCacheRepository : ICacheRepository
{
    public Task AddAsync<T>(string key, T value, TimeSpan expiration) => Task.CompletedTask;

    public Task RemoveAsync(string key) => Task.CompletedTask;

    public Task<(bool Success, T? Value)> TryGetValueAsync<T>(string key)
        => Task.FromResult<(bool, T?)>((false, default));
}
