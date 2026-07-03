using System.Text.Json;
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
        
        _producer =  new ProducerBuilder<string, string>(config).Build();
    }
    public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(message);
        
        try
        {
            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = key,
                    Value = payload
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