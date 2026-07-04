using System.Text;
using Confluent.Kafka;
using BookingService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Domain.Entities;
using Shared.Domain.Settings;

namespace BookingService.Api.BackgroundServices;

public class BookingsKafkaConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaSettings> settings,
    ILogger<BookingsKafkaConsumer> logger) : BackgroundService
{
    private const string Topic = "bookings";
    private const string ConsumerGroup = "booking-service";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            GroupId = ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        }).Build();

        consumer.Subscribe(Topic);
        logger.LogInformation("Subscribed to {Topic}", Topic);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await Task.Run(() => consumer.Consume(ct), ct);
                    if (result?.Message is null) continue;

                    await SaveToInboxAsync(result, ct);

                    consumer.StoreOffset(result);
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error");
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(PollInterval, ct);
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task SaveToInboxAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventType = GetHeader(result.Message.Headers, "event-type");
        var messageId = GetHeader(result.Message.Headers, "message-id");

        var inbox = new InboxMessage
        {
            Id = Guid.Parse(messageId),
            Topic = result.Topic,
            Partition = result.Partition.Value,
            Offset = result.Offset.Value,
            Key = result.Message.Key,
            Type = eventType,
            Payload = result.Message.Value
        };

        try
        {
            db.InboxMessages.Add(inbox);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateInbox(ex))
        {
            logger.LogDebug("Duplicate inbox message at {Topic}-{Partition}-{Offset}",
                result.Topic, result.Partition, result.Offset);
        }
    }

    private static bool IsDuplicateInbox(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;

    private static string? GetHeader(Headers headers, string key) =>
        headers.TryGetLastBytes(key, out var bytes) ? Encoding.UTF8.GetString(bytes) : null;
}
