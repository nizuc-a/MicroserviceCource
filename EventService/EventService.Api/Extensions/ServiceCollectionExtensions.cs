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
        
        var otlpEndpoint = configuration["Otlp:Endpoint"]
                           ?? throw new InvalidOperationException("Otlp:Endpoint is not configured.");
        var serviceName = configuration["Otlp:ServiceName"]
                          ?? throw new InvalidOperationException("Otlp:ServiceName is not configured.");

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }
}
