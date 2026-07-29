using EventService.Domain.Settings;
using Microsoft.Extensions.Options;

namespace EventService.IntegrationTests;

internal static class TestRedisSettings
{
    public static IOptions<RedisSettings> Default { get; } =
        Options.Create(new RedisSettings
        {
            EventTtlMinutes = 1,
            TopTtlMinutes = 5
        });
}
