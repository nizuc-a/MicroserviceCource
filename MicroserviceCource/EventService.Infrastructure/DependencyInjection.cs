using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Application.Abstractions.TaskQueue;
using EventService.Infrastructure.Repository;
using EventService.Infrastructure.Services;
using EventService.Infrastructure.TaskQueue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, Services.EventService>();
        services.AddScoped<IEventRepository, EventRepository>();
        
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingService, BookingService>();
        
        services.AddSingleton<IBookingTaskQueue, InMemoryBookingTaskQueue>();
        
        return services;
    }
    
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        return services;
    }
}