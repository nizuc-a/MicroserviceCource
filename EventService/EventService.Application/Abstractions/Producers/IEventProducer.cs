namespace EventService.Application.Abstractions.Producers;

public interface IEventProducer
{
    Task PublishAsync(
        string topic,
        string key,
        string type,
        string messageId,
        string payload,
        CancellationToken ct = default);
}