using BookingService.Api.Middleware;
using BookingService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseBookingServiceApi(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
