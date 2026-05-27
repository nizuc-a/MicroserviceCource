using EventService.Domain.Entities;
using EventService.Infrastructure.Repository;
using EventService.Infrastructure.Services;
using EventService.IntegrationTests.DatabaseFixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventService.IntegrationTests;

[Collection("Database")]
public class BookRepositoryTests
{
    private readonly PostgreSqlContainerFixture _container;

    public BookRepositoryTests(PostgreSqlContainerFixture container)
    {
        _container = container;
    }

    [Fact]
    public async Task CreateBooking()
    {
        await _container.ResetDatabaseAsync();
        
        await using var context = _container.CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        
        var bookingRepository = new BookingRepository(context);
        var service = new BookingService(bookingRepository);
        
        await service.CreateBookingAsync(eventEntity.Id);
        
        await using var verifyContext  = _container.CreateContext();
        var booking = await verifyContext.Bookings.FirstAsync();
        
        Assert.NotNull(booking);
        Assert.Equal(eventEntity.Id, booking.EventId);
    }
    
    [Fact]
    public async Task CreateBooking_DbUpdateException_IdenticalIds()
    {
        await _container.ResetDatabaseAsync();
        
        await using var context = _container.CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        
        var bookingRepository = new BookingRepository(context);
        
        var booking = await bookingRepository.CreateBookingAsync(eventEntity.Id);
        
        await using var verifyContext  = _container.CreateContext();
        
        verifyContext.Bookings.Add(booking);
        
        await Assert.ThrowsAsync<DbUpdateException>(() => verifyContext.SaveChangesAsync());
    }
    
    [Fact]
    public async Task GetBooking()
    {
        await _container.ResetDatabaseAsync();
        
        await using var context = _container.CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        
        var bookingRepository = new BookingRepository(context);
        
        var booking = await bookingRepository.CreateBookingAsync(eventEntity.Id);
        
        await using var verifyContext  = _container.CreateContext();
        bookingRepository = new BookingRepository(verifyContext);
        
        var verifyBooking = await bookingRepository.GetBookingByIdAsync(booking.Id);
        
        Assert.NotNull(verifyBooking);
        Assert.Equal(eventEntity.Id, verifyBooking.EventId);
    }
    
    [Fact]
    public async Task CreateBooking_WithInvalidEventId_ThrowsForeignKeyViolation()
    {
        await _container.ResetDatabaseAsync();
        
        await using var context = _container.CreateContext();
        var nonExistentEventId = Guid.NewGuid();
        var booking = new Booking(nonExistentEventId);
        context.Bookings.Add(booking);
        
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}