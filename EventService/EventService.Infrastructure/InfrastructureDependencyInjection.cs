using EventService.Application.Abstractions.Producers;
using EventService.Application.Abstractions.Repositories;
using EventService.Infrastructure.DbContext;
using EventService.Infrastructure.Producers;
using EventService.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICacheRepository, RedisRepository>();
        services.AddScoped<IEventRepository, EventRepository>();

        services.AddSingleton<IEventProducer, KafkaEventProducer>();

        return services;
    }
}