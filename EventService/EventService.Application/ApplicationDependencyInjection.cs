using EventService.Application.Abstractions.IntegrationEvents;
using EventService.Application.Abstractions.Services;
using EventService.Application.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, Services.EventService>();
        services.AddScoped<IIntegrationEventHandler, IntegrationEventHandler>();
        
        return services;
    }
}