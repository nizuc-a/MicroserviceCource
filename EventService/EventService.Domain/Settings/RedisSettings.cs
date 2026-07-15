namespace EventService.Domain.Settings;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string Host { get; set; } = "localhost:6379";
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 3000;
    public bool AbortOnConnectFail { get; set; } = false;
    public int ConnectRetry { get; set; } = 3;

    /// <summary>TTL for a single event cache entry (key event:{id}).</summary>
    public int EventTtlMinutes { get; set; } = 1;

    /// <summary>TTL for the top-events aggregate (key events:top10).</summary>
    public int TopTtlMinutes { get; set; } = 5;
}