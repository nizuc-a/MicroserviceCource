using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.TaskQueue;
using EventService.Domain.Enums;

namespace EventService.Api.BackgroundServices;

public class BookingBackgroundService(
    IBookingTaskQueue taskQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);
    
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BookingBackgroundService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookings = taskQueue.GetPending().ToList();
                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking.Id, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка обработки брони");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("BookingBackgroundService остановлен");
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            

            var booking = await bookingRepository.GetBookingByIdAsync(bookingId, stoppingToken);
            if (booking == null || booking.Status != BookingStatus.Pending)
                return;

            var @event = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);
            if (@event == null)
            {
                booking.Reject();
                await bookingRepository.SaveChangesAsync(stoppingToken);

                logger.LogWarning(
                    "Booking {BookingId} rejected: event {EventId} not found",
                    booking.Id, booking.EventId);

                return;
            }

            booking.Confirm();
            await bookingRepository.SaveChangesAsync(stoppingToken);

            logger.LogInformation(
                "Booking {BookingId} for event {EventId} processed → {Status}",
                booking.Id, booking.EventId, booking.Status);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var booking = await bookingRepository.GetBookingByIdAsync(bookingId, stoppingToken);
                if (booking != null)
                {
                    booking.Reject();

                    var @event = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);
                    if (@event != null)
                        @event.ReleaseSeats();

                    await eventRepository.SaveChangesAsync(stoppingToken);
                }

                logger.LogError(ex,
                    "Booking {BookingId} rejected due to processing error",
                    bookingId);
            }
            catch (Exception releaseEx)
            {
                logger.LogError(releaseEx,
                    "Failed to reject booking {BookingId} after error",
                    bookingId);
            }
        }
    }
}