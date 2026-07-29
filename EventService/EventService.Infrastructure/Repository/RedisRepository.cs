using System.Text.Json;
using EventService.Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventService.Infrastructure.Repository;

public class RedisRepository : ICacheRepository
{
    private readonly ILogger<RedisRepository> _logger;
    private readonly IDatabase _redis;

    public RedisRepository(IConnectionMultiplexer multiplexer, ILogger<RedisRepository> logger)
    {
        _logger = logger;
        _redis = multiplexer.GetDatabase();
    }

    public async Task AddAsync<T>(string key, T value, TimeSpan expiration)
    {
        var json = JsonSerializer.Serialize(value);

        try
        {
            await _redis.StringSetAsync(key, json, expiration);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Ошибка записи сущности по ключу {Key} в Redis", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _redis.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Ошибка удаления по ключу {Key} из Redis", key);
        }
    }

    public async Task<(bool Success, T? Value)> TryGetValueAsync<T>(string key)
    {
        try
        {
            var redisValue = await _redis.StringGetAsync(key);

            if (!redisValue.HasValue)
                return (false, default);

            var result = JsonSerializer.Deserialize<T>(redisValue.ToString());

            return (true, result);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Ошибка чтения по ключу {Key} из Redis", key);
            return (false, default);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ошибка десериализации по ключу {Key} из Redis", key);
            return (false, default);
        }
    }
}
