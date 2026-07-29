using EventService.Api.Middleware;
using EventService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace EventService.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseEventServiceApi(this WebApplication app)
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
        app.MapPrometheusScrapingEndpoint();

        return app;
    }
}
