using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Abstractions.Services;
using UserService.Application.Services;

namespace UserService.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        
        return services;
    }
}