using EventService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventService.IntegrationTests.DatabaseFixtures;

public class PostgreSqlContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container {get; private set; } = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventapi")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    
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

        return context;
    }

    public async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}