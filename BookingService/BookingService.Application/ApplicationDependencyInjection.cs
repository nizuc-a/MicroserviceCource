using BookingService.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService.Application.Services.BookingService>();
        
        return services;
    }
}