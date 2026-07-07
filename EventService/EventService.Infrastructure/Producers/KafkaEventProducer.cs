using Confluent.Kafka;
using EventService.Application.Abstractions.Producers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Domain.Settings;

namespace EventService.Infrastructure.Producers;

public class KafkaEventProducer : IEventProducer, IDisposable
{
    private readonly ILogger<KafkaEventProducer> _logger;
    private readonly IProducer<string, string> _producer;

    public KafkaEventProducer(IOptions<KafkaSettings> settings, ILogger<KafkaEventProducer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string topic, string key, string type, string messageId, string payload,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = key,
                    Value = payload,
                    Headers = new Headers
                    {
                        { "event-type", System.Text.Encoding.UTF8.GetBytes(type) },
                        { "message-id", System.Text.Encoding.UTF8.GetBytes(messageId) },
                    }
                },
                ct);

            _logger.LogInformation(
                "Published to {Topic}, partition {Partition}, offset {Offset}",
                result.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish to {Topic}", topic);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}