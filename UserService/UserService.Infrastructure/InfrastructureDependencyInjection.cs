using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Abstractions.Auth;
using UserService.Application.Abstractions.Repositories;
using UserService.Infrastructure.Auth;
using UserService.Infrastructure.DbContext;
using UserService.Infrastructure.Repository;

namespace UserService.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        services.AddScoped<IUserRepository, UserRepository>();
        
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, Sha256PasswordHasher>();

        return services;
    }
}