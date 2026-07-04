using BookingService.Application.Abstractions.IntegrationEvents;
using BookingService.Application.Abstractions.Services;
using BookingService.Application.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService.Application.Services.BookingService>();
        services.AddScoped<IIntegrationEventHandler, IntegrationEventHandler>();
        
        return services;
    }
}