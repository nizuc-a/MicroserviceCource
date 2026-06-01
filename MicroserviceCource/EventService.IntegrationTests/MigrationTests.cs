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

        var context = _container.CreateContext();
        
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations); 
    }
    
    [Fact]
    public void PrimaryKeys_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();
        
        var eventEntityType = context.Model.FindEntityType(typeof(Event));
        var eventPrimaryKey = eventEntityType.FindPrimaryKey();
        Assert.NotNull(eventPrimaryKey);
        Assert.Equal("Id", eventPrimaryKey.Properties.First().Name);
        
        var bookingEntityType = context.Model.FindEntityType(typeof(Booking));
        var bookingPrimaryKey = bookingEntityType.FindPrimaryKey();
        Assert.NotNull(bookingPrimaryKey);
        Assert.Equal("Id", bookingPrimaryKey.Properties.First().Name);
    }
    
    [Fact]
    public void ForeignKey_IsConfiguredCorrectly()
    {
        using var context = _container.CreateContext();
        
        var bookingEntityType = context.Model.FindEntityType(typeof(Booking));
        var foreignKey = bookingEntityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Event));
        
        Assert.NotNull(foreignKey);
        Assert.Equal("EventId", foreignKey.Properties.First().Name);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }
    
    [Fact]
    public void Indexes_AreConfiguredCorrectly()
    {
        using var context = _container.CreateContext();
        
        var bookingEntityType = context.Model.FindEntityType(typeof(Booking));
        var bookingIndexes = bookingEntityType.GetIndexes();
        Assert.Contains(bookingIndexes, i => i.GetDatabaseName()?.ToLower().Contains("event_id") == true);
        
        var eventEntityType = context.Model.FindEntityType(typeof(Event));
        var eventIndexes = eventEntityType.GetIndexes();
        Assert.Contains(eventIndexes, i => i.GetDatabaseName()?.ToLower().Contains("start_at") == true);
        Assert.Contains(eventIndexes, i => i.GetDatabaseName()?.ToLower().Contains("end_at") == true);
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
        var navigationToBookings = eventEntityType.GetNavigations()
            .FirstOrDefault(n => n.Name == "Bookings");
        Assert.NotNull(navigationToBookings);
        
        var bookingEntityType = context.Model.FindEntityType(typeof(Booking));
        var navigationToEvent = bookingEntityType.GetNavigations()
            .FirstOrDefault(n => n.Name == "Event");
        Assert.NotNull(navigationToEvent);
    }
}