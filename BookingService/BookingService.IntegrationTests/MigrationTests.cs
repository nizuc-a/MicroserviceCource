using BookingService.Domain.Entities;
using BookingService.IntegrationTests.DatabaseFixtures;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;
using Xunit;

namespace BookingService.IntegrationTests;

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

        Assert.Equal(2, appliedMigrations.Count);
        Assert.Equal("20260704090827_InitialCreate", appliedMigrations[0]);
        Assert.Equal("20260704201812_AddSeatCountToBooking", appliedMigrations[1]);
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

        Assert.Equal(["bookings", "inbox_messages", "outbox_messages"], tableNames);
    }

    [Fact]
    public void PrimaryKeys_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();

        var bookingEntityType = context.Model.FindEntityType(typeof(Booking));
        Assert.NotNull(bookingEntityType);
        var bookingPrimaryKey = bookingEntityType.FindPrimaryKey();
        Assert.NotNull(bookingPrimaryKey);
        Assert.Equal("Id", bookingPrimaryKey.Properties.First().Name);

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

        var bookingEntityType = context.Model.FindEntityType(typeof(Booking));
        Assert.NotNull(bookingEntityType);
        var bookingIndexes = bookingEntityType.GetIndexes();
        Assert.Contains(bookingIndexes, i => i.GetDatabaseName()?.ToLower().Contains("event_id") == true);
        Assert.Contains(bookingIndexes, i => i.GetDatabaseName()?.ToLower().Contains("user_id") == true);

        var inboxEntityType = context.Model.FindEntityType(typeof(InboxMessage));
        Assert.NotNull(inboxEntityType);
        var inboxIndexes = inboxEntityType.GetIndexes();
        Assert.Contains(inboxIndexes, i => i.GetDatabaseName()?.ToLower().Contains("topic_partition_offset") == true);
    }

    [Fact]
    public async Task CheckConstraints_AreConfiguredCorrectly()
    {
        await _container.ResetDatabaseAsync();

        await using var context = _container.CreateContext();

        var constraintNames = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT conname AS "Value"
                FROM pg_constraint
                WHERE conrelid = 'bookings'::regclass AND contype = 'c'
                """)
            .ToListAsync();

        Assert.Contains(constraintNames, c => c.ToLower().Contains("createdbeforeprocessed"));
        Assert.Contains(constraintNames, c => c.ToLower().Contains("seat_count"));
    }
}
