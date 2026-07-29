using EventService.Api.Services;
using EventService.Application;
using EventService.Domain.Settings;
using EventService.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Api;
using Shared.Domain.Settings;

namespace EventService.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventServiceApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        var jwtSettings = configuration
                              .GetSection(JwtSettings.SectionName)
                              .Get<JwtSettings>()
                          ?? throw new InvalidOperationException("Jwt settings not configured.");

        services.AddApiControllers(withJsonEnumConverter: false);
        services.AddAuthorization();
        services.AddApplicationServices();
        services.AddInfrastructureServices(connectionString);
        services.AddJwtAuthentication(jwtSettings);
        services.AddApplicationHostedServices();
        services.AddSharedAppSettings(configuration);
        services.AddJwtBearerSwaggerGen("Event Service API");

        services.AddRedis(configuration);
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
        
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("EventService"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = new Uri(configuration["Otlp:Endpoint"]!));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }
}
