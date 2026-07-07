using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shared.Domain.Entities;
using UserService.Domain.Entities;
using UserService.IntegrationTests.DatabaseFixtures;
using Xunit;

namespace UserService.IntegrationTests;

[Collection("Database")]
public class MigrationTests
{
    private readonly PostgreSqlContainerFixture _container;

    public MigrationTests(PostgreSqlContainerFixture container)
    {
        _container = container;
    }

    [Fact]
    public async Task AllMigrationApplied()
    {
        await _container.ResetDatabaseAsync();

        await using var context = _container.CreateContext();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task AllMigrations_AreApplied_InCorrectOrder()
    {
        await _container.ResetDatabaseAsync();

        await using var context = _container.CreateContext();

        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();

        Assert.Single(appliedMigrations);
        Assert.Equal("20260704091338_InitialCreate", appliedMigrations[0]);
    }

    [Fact]
    public async Task Database_ContainsExpectedTables()
    {
        await _container.ResetDatabaseAsync();

        await using var context = _container.CreateContext();

        var tableNames = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE'
                  AND table_name NOT LIKE '__EFMigrationsHistory%'
                ORDER BY table_name
                """)
            .ToListAsync();

        Assert.Equal(["inbox_messages", "outbox_messages", "users"], tableNames);
    }

    [Fact]
    public void PrimaryKeys_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();

        var userEntityType = context.Model.FindEntityType(typeof(User));
        Assert.NotNull(userEntityType);
        var userPrimaryKey = userEntityType.FindPrimaryKey();
        Assert.NotNull(userPrimaryKey);
        Assert.Equal("Id", userPrimaryKey.Properties.First().Name);

        var inboxEntityType = context.Model.FindEntityType(typeof(InboxMessage));
        Assert.NotNull(inboxEntityType);
        Assert.NotNull(inboxEntityType.FindPrimaryKey());

        var outboxEntityType = context.Model.FindEntityType(typeof(OutboxMessage));
        Assert.NotNull(outboxEntityType);
        Assert.NotNull(outboxEntityType.FindPrimaryKey());
    }

    [Fact]
    public void Indexes_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();

        var userEntityType = context.Model.FindEntityType(typeof(User));
        Assert.NotNull(userEntityType);
        var loginIndex = userEntityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == "Login"));

        Assert.NotNull(loginIndex);
        Assert.True(loginIndex.IsUnique);

        var inboxEntityType = context.Model.FindEntityType(typeof(InboxMessage));
        Assert.NotNull(inboxEntityType);
        var inboxIndexes = inboxEntityType.GetIndexes();
        Assert.Contains(inboxIndexes, i => i.GetDatabaseName()?.ToLower().Contains("topic_partition_offset") == true);
    }
}
