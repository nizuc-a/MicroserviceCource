using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Application.Abstractions.TaskQueue;
using EventService.Application.Services;
using EventService.Infrastructure.Repository;
using EventService.Infrastructure.TaskQueue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingService, BookingService>();
        
        services.AddSingleton<IBookingTaskQueue, InMemoryBookingTaskQueue>();
        
        return services;
    }
}