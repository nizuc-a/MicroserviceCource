using EventService.Api.BackgroundServices;

namespace EventService.Api.Services;

public static class HostedServices
{
    public static IServiceCollection AddApplicationHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<BookingsKafkaConsumer>();
        services.AddHostedService<InboxProcessor>();
        services.AddHostedService<OutboxProcessor>();
        
        return services;
    }
}