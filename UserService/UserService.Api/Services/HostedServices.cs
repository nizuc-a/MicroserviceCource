using UserService.Api.BackgroundServices;

namespace UserService.Api.Services;

public static class HostedServices
{
    public static IServiceCollection AddApplicationHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<BookingsKafkaConsumer>();
        services.AddHostedService<InboxProcessor>();

        return services;
    }
}
