using EventService.Application.Abstractions.Auth;
using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.TaskQueue;
using EventService.Infrastructure.Auth;
using EventService.Infrastructure.DbContext;
using EventService.Infrastructure.Repository;
using EventService.Infrastructure.TaskQueue;
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

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, Sha256PasswordHasher>();
        
        services.AddSingleton<IBookingTaskQueue, InMemoryBookingTaskQueue>();

        return services;
    }
}