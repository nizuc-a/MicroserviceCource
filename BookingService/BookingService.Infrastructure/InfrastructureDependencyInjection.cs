using BookingService.Application.Abstractions.Producers;
using BookingService.Application.Abstractions.Repository;
using BookingService.Application.IntegrationEvents;
using BookingService.Infrastructure.DbContext;
using BookingService.Infrastructure.IntegrationEvents;
using BookingService.Infrastructure.Producers;
using BookingService.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IIntegrationEventHandler, IntegrationEventHandler>();
        
        services.AddSingleton<IEventProducer, KafkaEventProducer>();

        return services;
    }
}