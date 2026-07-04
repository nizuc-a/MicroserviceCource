using Shared.Api;
using Shared.Domain.Settings;
using UserService.Api.Services;
using UserService.Application;
using UserService.Infrastructure;

namespace UserService.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserServiceApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        var jwtSettings = configuration
                              .GetSection(JwtSettings.SectionName)
                              .Get<JwtSettings>()
                          ?? throw new InvalidOperationException("Jwt settings not configured.");

        services.AddApiControllers();
        services.AddAuthorization();
        services.AddApplicationServices();
        services.AddInfrastructureServices(connectionString);
        services.AddJwtAuthentication(jwtSettings);
        services.AddApplicationHostedServices();
        services.AddSharedAppSettings(configuration);
        services.AddJwtBearerSwaggerGen("User Service API");

        return services;
    }
}
