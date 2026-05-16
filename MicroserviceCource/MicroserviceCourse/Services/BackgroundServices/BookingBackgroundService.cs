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
        var delay = TimeSpan.FromSeconds(2);

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

            await Task.Delay(delay, stoppingToken);
        }

        logger.LogInformation("BookingBackgroundService остановлен");
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Начата обработка брони {TaskId}, статус: {Status}",
            booking.Id, booking.Status);

        if (booking.Status != BookingStatus.Pending)
        {
            logger.LogWarning("Бронь {BookingId} уже обработана, статус: {Status}",
                booking.Id, booking.Status);
            return;
        }

        var delay = TimeSpan.FromSeconds(2);

        await Task.Delay(delay, stoppingToken);

        using var scope = scopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        await _processingSemaphore.WaitAsync(stoppingToken);

        Event? eventEntity = null;
        Booking? bookingDb = null;  
        try
        {
            try
            {
                bookingDb = await bookingService.GetBookingByIdAsync(booking.Id, stoppingToken);
                eventEntity = await eventService.GetById(booking.EventId, stoppingToken);
            }
            catch (KeyNotFoundException) when (bookingDb is null)
            {
                logger.LogWarning("Бронь {BookingId} не найдена", booking.Id);
                
                return;
            }
            catch (KeyNotFoundException) when (eventEntity is null)
            {
                logger.LogWarning("Событие {EventId} для брони {BookingId} не найдено",
                    booking.EventId, booking.Id);

                bookingDb.Reject();
                eventEntity?.ReleaseSeats();

                return;
            }
            
            bookingDb.Confirm();
            logger.LogInformation("Бронь {BookingId} успешно подтверждена", booking.Id);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            bookingDb.Reject();
            eventEntity?.ReleaseSeats();

            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Необработанная ошибка при обработке брони {BookingId}", booking.Id);

            bookingDb.Reject();
            eventEntity?.ReleaseSeats();

            throw;
        }
        finally
        {
            await eventService.SaveChangesAsync(stoppingToken);
            await bookingService.SaveChangesAsync(stoppingToken);

            _processingSemaphore.Release();
        }

        logger.LogInformation("Бронь {TaskId} успешно обработана", booking.Id);
    }
}