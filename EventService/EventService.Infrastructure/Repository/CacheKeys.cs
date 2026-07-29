namespace EventService.Infrastructure.Repository;

internal static class CacheKeys
{
    public static string EventById(Guid id) => $"event:{id}";

    public static string TopEvents(int count) => $"events:top{count}";
}
