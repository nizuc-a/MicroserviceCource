using BookingService.Application.Abstractions.IntegrationEvents;
using BookingService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Api.BackgroundServices;

public class InboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<InboxProcessor> logger) : BackgroundService
{
    private const int BatchSize = 50;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await ProcessBatchAsync(ct);
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler>();

        var messages = await db.InboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.ReceivedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var msg in messages)
        {
            try
            {
                await handler.HandleAsync(msg, ct);
                msg.ProcessedAt = DateTime.UtcNow;
                msg.Error = null;
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                msg.Error = ex.Message;
                logger.LogError(ex, "Failed to process inbox {Id}", msg.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}