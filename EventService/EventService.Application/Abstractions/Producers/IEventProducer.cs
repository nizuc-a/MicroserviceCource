namespace EventService.Application.Abstractions.Producers;

public interface IEventProducer
{
    Task PublishAsync<T>(
        string topic,
        string key,
        T message,
        CancellationToken ct = default);
}