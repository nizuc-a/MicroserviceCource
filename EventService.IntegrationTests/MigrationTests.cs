using EventService.Domain.Entities;
using EventService.IntegrationTests.DatabaseFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
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

        Assert.Equal(3, appliedMigrations.Count);
        Assert.Equal("20260517111409_InitialCreate", appliedMigrations[0]);
        Assert.Equal("20260617131810_AddUsersTable", appliedMigrations[1]);
        Assert.Equal("20260619072543_AddUniqueAttributeInLogin", appliedMigrations[2]);
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

        Assert.Equal(["bookings", "events", "users"], tableNames);
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
        
        var bookingEntityType = context.Model.FindEntityType(typeof(Booking));
        Assert.NotNull(bookingEntityType);
        var bookingPrimaryKey = bookingEntityType.FindPrimaryKey();
        Assert.NotNull(bookingPrimaryKey);
        Assert.Equal("Id", bookingPrimaryKey.Properties.First().Name);

        var userEntityType = context.Model.FindEntityType(typeof(User));
        Assert.NotNull(userEntityType);
        var userPrimaryKey = userEntityType.FindPrimaryKey();
        Assert.NotNull(userPrimaryKey);
        Assert.Equal("Id", userPrimaryKey.Properties.First().Name);
    }
    
    [Fact]
    public void ForeignKey_IsConfiguredCorrectly()
    {
        using var context = _container.CreateContext();
        
        var bookingEntityType = context.Model.FindEntityType(typeof(Booking));
        Assert.NotNull(bookingEntityType);

        var eventForeignKey = bookingEntityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Event));
        
        Assert.NotNull(eventForeignKey);
        Assert.Equal("EventId", eventForeignKey.Properties.First().Name);
        Assert.Equal(DeleteBehavior.Cascade, eventForeignKey.DeleteBehavior);

        var userForeignKey = bookingEntityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(User));

        Assert.NotNull(userForeignKey);
        Assert.Equal("UserId", userForeignKey.Properties.First().Name);
        Assert.Equal(DeleteBehavior.Cascade, userForeignKey.DeleteBehavior);
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
        
        var eventEntityType = context.Model.FindEntityType(typeof(Event));
        Assert.NotNull(eventEntityType);
        var eventIndexes = eventEntityType.GetIndexes();
        Assert.Contains(eventIndexes, i => i.GetDatabaseName()?.ToLower().Contains("start_at") == true);
        Assert.Contains(eventIndexes, i => i.GetDatabaseName()?.ToLower().Contains("end_at") == true);

        var userEntityType = context.Model.FindEntityType(typeof(User));
        Assert.NotNull(userEntityType);
        var loginIndex = userEntityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == "Login"));

        Assert.NotNull(loginIndex);
        Assert.True(loginIndex.IsUnique);
    }
    
    [Fact]
    public void CheckConstraints_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();
    
        var designTimeModel = context.GetService<IDesignTimeModel>();
        var model = designTimeModel.Model;
    
        var eventEntityType = model.FindEntityType(typeof(Event));
        var eventConstraints = eventEntityType.GetCheckConstraints();
        Assert.Contains(eventConstraints, c => c.Name?.ToLower().Contains("startbeforeend") == true);
    
        var bookingEntityType = model.FindEntityType(typeof(Booking));
        var bookingConstraints = bookingEntityType.GetCheckConstraints();
        Assert.Contains(bookingConstraints, c => c.Name?.ToLower().Contains("createdbeforeprocessed") == true);
    }
    
    [Fact]
    public void NavigationProperties_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();
        
        var eventEntityType = context.Model.FindEntityType(typeof(Event));
        Assert.NotNull(eventEntityType);
        var navigationToBookings = eventEntityType.GetNavigations()
            .FirstOrDefault(n => n.Name == "Bookings");
        Assert.NotNull(navigationToBookings);
        
        var bookingEntityType = context.Model.FindEntityType(typeof(Booking));
        Assert.NotNull(bookingEntityType);
        var navigationToEvent = bookingEntityType.GetNavigations()
            .FirstOrDefault(n => n.Name == "Event");
        Assert.NotNull(navigationToEvent);

        var navigationToUser = bookingEntityType.GetNavigations()
            .FirstOrDefault(n => n.Name == "User");
        Assert.NotNull(navigationToUser);

        var userEntityType = context.Model.FindEntityType(typeof(User));
        Assert.NotNull(userEntityType);
        var userNavigationToBookings = userEntityType.GetNavigations()
            .FirstOrDefault(n => n.Name == "Bookings");
        Assert.NotNull(userNavigationToBookings);
    }
}