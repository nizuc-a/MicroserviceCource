using BookingService.Application.Abstractions.Producers;
using BookingService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Api.BackgroundServices;

public class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IEventProducer producer,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 50;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing loop failed");
            }
            
            await Task.Delay(PollInterval, stoppingToken);
        }
    }
    
    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);
        
        foreach (var msg in messages)
        {
            try
            {
                await producer.PublishAsync(msg.Topic, msg.Key, msg.Type, msg.Id.ToString("D"), msg.Payload, ct);
                msg.ProcessedAt = DateTime.UtcNow;
                msg.Error = null;
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                msg.Error = ex.Message;
                logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
            }
        }
        await db.SaveChangesAsync(ct);
    }
}