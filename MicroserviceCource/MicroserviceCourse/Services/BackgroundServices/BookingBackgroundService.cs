using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Interfaces.TaskQueue;
using MicroserviceCourse.Model.Enum;

namespace MicroserviceCourse.Services.BackgroundServices;

public class BookingBackgroundService(IBookingTaskQueue taskQueue, IServiceScopeFactory scopeFactory, ILogger<BookingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BookingBackgroundService запущен");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (taskQueue.TryDequeue(out var booking))
                {
                    logger.LogInformation(
                        "Начата обработка брони {TaskId}, статус: {ReportType}",
                        booking.Id, booking.Status);

                    await Task.Delay(5000, stoppingToken);

                    using var scope = scopeFactory.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                    await bookingService.UpdateStatusAsync(booking.Id, BookingStatus.Confirmed, stoppingToken);

                    logger.LogInformation("Бронь {TaskId} успешно обработана", booking.Id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка обработки брони");
                throw;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
        
        logger.LogInformation("BookingBackgroundService остановлен");
    }
}