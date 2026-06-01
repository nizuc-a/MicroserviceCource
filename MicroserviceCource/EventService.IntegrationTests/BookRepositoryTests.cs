using System.Net;
using EventService.Application.Services;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;
using EventService.Infrastructure.Repository;
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
        var eventRepository = new EventRepository(context);
        var service = new BookingService(bookingRepository, eventRepository);

        await service.CreateBookingAsync(eventEntity.Id);

        await using var verifyContext = _container.CreateContext();
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
        var booking = new Booking(eventEntity.Id);

        await bookingRepository.CreateBookingAsync(booking);

        await using var verifyContext = _container.CreateContext();

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
        var booking = new Booking(eventEntity.Id);

        await bookingRepository.CreateBookingAsync(booking);

        await using var verifyContext = _container.CreateContext();
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

    [Fact]
    public async Task CreateBooking_ConcurrentRequests_NoOverbooking()
    {
        await _container.ResetDatabaseAsync();

        const int totalSeats = 5;

        await using var context = _container.CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), totalSeats);
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        
        var successfulBookings = 0;
        var failedBookings = 0;

        var tasks = Enumerable.Range(0, totalSeats * 3)
            .Select(_ => Task.Run(async () =>
            {
                await using var context = _container.CreateContext();
                var bookingRepository = new BookingRepository(context);
                var eventRepository = new EventRepository(context);
                var bookingService = new BookingService(bookingRepository, eventRepository);

                try
                {
                    await bookingService.CreateBookingAsync(eventEntity.Id);
                    Interlocked.Increment(ref successfulBookings);
                }
                catch (NoAvailableSeatsException)
                {
                    Interlocked.Increment(ref failedBookings);
                }
            }));

        await Task.WhenAll(tasks);

        Assert.Equal(5, successfulBookings);
        Assert.Equal(10, failedBookings);
    }
}