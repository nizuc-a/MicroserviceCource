using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Abstractions.IntegrationEvents;
using UserService.Application.Abstractions.Services;
using UserService.Application.IntegrationEvents;
using UserService.Application.Services;

namespace UserService.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIntegrationEventHandler, IntegrationEventHandler>();
        
        
        return services;
    }
}