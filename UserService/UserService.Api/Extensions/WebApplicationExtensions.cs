using Microsoft.EntityFrameworkCore;
using UserService.Api.Middleware;
using UserService.Infrastructure.DbContext;

namespace UserService.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseUserServiceApi(this WebApplication app)
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
