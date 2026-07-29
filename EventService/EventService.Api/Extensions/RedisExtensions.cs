using EventService.Domain.Settings;
using StackExchange.Redis;

namespace EventService.Api.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisSettings = configuration
                              .GetSection(RedisSettings.SectionName)
                              .Get<RedisSettings>()
                          ?? throw new InvalidOperationException("Redis settings not configured.");
        
        var options = new ConfigurationOptions
        {
            EndPoints = { redisSettings.Host },
            ConnectTimeout = redisSettings.ConnectTimeout,
            SyncTimeout = redisSettings.SyncTimeout,
            AbortOnConnectFail = redisSettings.AbortOnConnectFail,
            ConnectRetry = redisSettings.ConnectRetry,
        };
        
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options)); 

        return services;
    }
}