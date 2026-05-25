using EventService.Api.Data;
using EventService.Api.Model.Entity;
using EventService.Api.Repository;
using EventService.Api.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventService.IntegrationTests;

[Collection("Database")]
public class BookRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE");
    }

    [Fact]
    public async Task CreateBooking()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        
        var bookingRepository = new BookingRepository(context);
        var service = new BookingService(bookingRepository);
        
        await service.CreateBookingAsync(eventEntity.Id);
        
        await using var verifyContext  = CreateContext();
        var booking = await verifyContext.Bookings.FirstAsync();
        
        Assert.NotNull(booking);
        Assert.Equal(eventEntity.Id, booking.EventId);
    }
    
    [Fact]
    public async Task CreateBooking_DbUpdateException_IdenticalIds()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        
        var bookingRepository = new BookingRepository(context);
        
        var booking = await bookingRepository.CreateBookingAsync(eventEntity.Id);
        
        await using var verifyContext  = CreateContext();
        
        verifyContext.Bookings.Add(booking);
        
        await Assert.ThrowsAsync<DbUpdateException>(() => verifyContext.SaveChangesAsync());
    }
    
    [Fact]
    public async Task GetBooking()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        
        var bookingRepository = new BookingRepository(context);
        
        var booking = await bookingRepository.CreateBookingAsync(eventEntity.Id);
        
        await using var verifyContext  = CreateContext();
        bookingRepository = new BookingRepository(verifyContext);
        
        var verifyBooking = await bookingRepository.GetBookingByIdAsync(booking.Id);
        
        Assert.NotNull(verifyBooking);
        Assert.Equal(eventEntity.Id, verifyBooking.EventId);
    }
    
    [Fact]
    public async Task CreateBooking_WithInvalidEventId_ThrowsForeignKeyViolation()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        var nonExistentEventId = Guid.NewGuid();
        var booking = new Booking(nonExistentEventId);
        context.Bookings.Add(booking);
        
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}