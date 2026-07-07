using EventService.Domain.Entities;
using EventService.IntegrationTests.DatabaseFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.Domain.Entities;
using Xunit;

namespace EventService.IntegrationTests;

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
        Assert.Equal("20260704090852_InitialCreate", appliedMigrations[0]);
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

        Assert.Equal(["events", "inbox_messages", "outbox_messages"], tableNames);
    }

    [Fact]
    public void PrimaryKeys_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();

        var eventEntityType = context.Model.FindEntityType(typeof(Event));
        Assert.NotNull(eventEntityType);
        var eventPrimaryKey = eventEntityType.FindPrimaryKey();
        Assert.NotNull(eventPrimaryKey);
        Assert.Equal("Id", eventPrimaryKey.Properties.First().Name);

        var inboxEntityType = context.Model.FindEntityType(typeof(InboxMessage));
        Assert.NotNull(inboxEntityType);
        var inboxPrimaryKey = inboxEntityType.FindPrimaryKey();
        Assert.NotNull(inboxPrimaryKey);
        Assert.Equal("Id", inboxPrimaryKey.Properties.First().Name);

        var outboxEntityType = context.Model.FindEntityType(typeof(OutboxMessage));
        Assert.NotNull(outboxEntityType);
        var outboxPrimaryKey = outboxEntityType.FindPrimaryKey();
        Assert.NotNull(outboxPrimaryKey);
        Assert.Equal("Id", outboxPrimaryKey.Properties.First().Name);
    }

    [Fact]
    public void Indexes_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();

        var eventEntityType = context.Model.FindEntityType(typeof(Event));
        Assert.NotNull(eventEntityType);
        var eventIndexes = eventEntityType.GetIndexes();
        Assert.Contains(eventIndexes, i => i.GetDatabaseName()?.ToLower().Contains("start_at") == true);
        Assert.Contains(eventIndexes, i => i.GetDatabaseName()?.ToLower().Contains("end_at") == true);

        var inboxEntityType = context.Model.FindEntityType(typeof(InboxMessage));
        Assert.NotNull(inboxEntityType);
        var inboxIndexes = inboxEntityType.GetIndexes();
        Assert.Contains(inboxIndexes, i => i.GetDatabaseName()?.ToLower().Contains("topic_partition_offset") == true);

        var outboxEntityType = context.Model.FindEntityType(typeof(OutboxMessage));
        Assert.NotNull(outboxEntityType);
        var outboxIndexes = outboxEntityType.GetIndexes();
        Assert.Contains(outboxIndexes, i => i.GetDatabaseName()?.ToLower().Contains("occurred_at") == true);
        Assert.Contains(outboxIndexes, i => i.GetDatabaseName()?.ToLower().Contains("processed_at") == true);
    }

    [Fact]
    public void CheckConstraints_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();

        var designTimeModel = context.GetService<IDesignTimeModel>();
        var model = designTimeModel.Model;

        var eventEntityType = model.FindEntityType(typeof(Event));
        var eventConstraints = eventEntityType!.GetCheckConstraints();
        Assert.Contains(eventConstraints, c => c.Name?.ToLower().Contains("startbeforeend") == true);
    }
}
