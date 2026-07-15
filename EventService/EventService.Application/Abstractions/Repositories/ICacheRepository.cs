namespace EventService.Application.Abstractions.Repositories;

public interface ICacheRepository
{
    Task AddAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);
    Task<(bool Success, T? Value)> TryGetValueAsync<T>(string key);
}
