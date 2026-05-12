using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Interfaces.TaskQueue;
using MicroserviceCourse.Model.Entity;
using MicroserviceCourse.Model.Enum;

namespace MicroserviceCourse.Services.BackgroundServices;

public class BookingBackgroundService(
    IBookingTaskQueue taskQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BookingBackgroundService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookings = taskQueue.GetPending().ToList();
                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
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

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }

        logger.LogInformation("BookingBackgroundService остановлен");
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Начата обработка брони {TaskId}, статус: {ReportType}",
            booking.Id, booking.Status);

        if (booking.Status != BookingStatus.Pending)
        {
            logger.LogWarning("Бронь {BookingId} уже обработана, статус: {Status}",
                booking.Id, booking.Status);
            return;
        }

        await Task.Delay(2000, stoppingToken);

        using var scope = scopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        await _processingSemaphore.WaitAsync(stoppingToken);

        Event eventEntity = null;
        try
        {
            try
            {
               eventEntity = await eventService.GetById(booking.EventId, stoppingToken);
            }
            catch (KeyNotFoundException e)
            {
                logger.LogWarning("Событие {EventId} для брони {BookingId} не найдено",
                    booking.EventId, booking.Id);

                await bookingService.UpdateStatusAsync(booking.Id, BookingStatus.Rejected, stoppingToken);
                if(eventEntity != null)
                {
                    eventEntity.ReleaseSeats();
                    await  eventService.SaveChangesAsync(stoppingToken);
                }
                return;
            }

            await bookingService.UpdateStatusAsync(booking.Id, BookingStatus.Confirmed, stoppingToken);
            logger.LogInformation("Бронь {BookingId} успешно подтверждена", booking.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Необработанная ошибка при обработке брони {BookingId}", booking.Id);
            await bookingService.UpdateStatusAsync(booking.Id, BookingStatus.Rejected, stoppingToken);

            if(eventEntity != null)
            {
                eventEntity.ReleaseSeats();
                await  eventService.SaveChangesAsync(stoppingToken);
            }
            
            throw;
        }
        finally
        {
            _processingSemaphore.Release();
        }

        logger.LogInformation("Бронь {TaskId} успешно обработана", booking.Id);
    }
}