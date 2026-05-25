using EventService.Api.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventService.IntegrationTests.DatabaseFixtures;

public class PostgreSqlContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container {get; private set; } = new PostgreSqlBuilder("postgres:16-alpine").Build();
    
    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.StopAsync();
    }
    
    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Container.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.Migrate();

        return context;
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE");
    }
}