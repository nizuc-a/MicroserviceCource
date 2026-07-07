using BookingService.Api.BackgroundServices;

namespace BookingService.Api.Services;

public static class HostedServices
{
    public static IServiceCollection AddApplicationHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<EventsKafkaConsumer>();
        services.AddHostedService<BookingsKafkaConsumer>();
        services.AddHostedService<InboxProcessor>();
        services.AddHostedService<OutboxProcessor>();
        
        return services;
    }
}