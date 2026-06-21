using EventService.Application.Services;
using EventService.Domain.Entities;
using EventService.Domain.Enums;
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

    private async Task ResetDatabaseAsync() => await _container.ResetDatabaseAsync();

    [Fact]
    public async Task CreateBookingAsync_WithValidData_SavesSuccessfully()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context);
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context);

        var repository = new BookingRepository(context);
        var booking = new Booking(eventEntity.Id, user.Id);

        await repository.CreateBookingAsync(booking);

        await using var verifyContext = _container.CreateContext();
        var savedBooking = await verifyContext.Bookings.FindAsync(booking.Id);

        Assert.NotNull(savedBooking);
        Assert.Equal(eventEntity.Id, savedBooking.EventId);
        Assert.Equal(user.Id, savedBooking.UserId);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_WithInvalidEventId_ThrowsDbUpdateException()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context);
        var repository = new BookingRepository(context);
        var booking = new Booking(Guid.NewGuid(), user.Id);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateBookingAsync(booking));
    }

    [Fact]
    public async Task CreateBookingAsync_WithInvalidUserId_ThrowsDbUpdateException()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context);
        var repository = new BookingRepository(context);
        var booking = new Booking(eventEntity.Id, Guid.NewGuid());

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateBookingAsync(booking));
    }

    [Fact]
    public async Task CreateBookingAsync_DuplicatedId_ThrowsDbUpdateException()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context);
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context);

        var bookingId = Guid.NewGuid();
        var booking = new Booking(eventEntity.Id, user.Id) { Id = bookingId };

        var repository = new BookingRepository(context);
        await repository.CreateBookingAsync(booking);

        await using var verifyContext = _container.CreateContext();
        var duplicateBooking = new Booking(eventEntity.Id, user.Id) { Id = bookingId };
        var verifyRepository = new BookingRepository(verifyContext);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            verifyRepository.CreateBookingAsync(duplicateBooking));
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingBooking_ReturnsWithIncludes()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context, "booking_user");
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context, title: "Концерт");

        var booking = new Booking(eventEntity.Id, user.Id);
        var repository = new BookingRepository(context);
        await repository.CreateBookingAsync(booking);

        await using var verifyContext = _container.CreateContext();
        var verifyRepository = new BookingRepository(verifyContext);

        var result = await verifyRepository.GetBookingByIdAsync(booking.Id);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(eventEntity.Id, result.EventId);
        Assert.Equal(user.Id, result.UserId);
        Assert.NotNull(result.Event);
        Assert.Equal("Концерт", result.Event.Title);
        Assert.NotNull(result.User);
        Assert.Equal("booking_user", result.User.Login);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NonExisting_ReturnsNull()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new BookingRepository(context);

        var result = await repository.GetBookingByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBookingsByUserId_ReturnsOnlyUserBookings()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user1 = await IntegrationTestDataHelper.SeedUserAsync(context, "user1");
        var user2 = await IntegrationTestDataHelper.SeedUserAsync(context, "user2");
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context);

        var repository = new BookingRepository(context);
        await repository.CreateBookingAsync(new Booking(eventEntity.Id, user1.Id));
        await repository.CreateBookingAsync(new Booking(eventEntity.Id, user1.Id));
        await repository.CreateBookingAsync(new Booking(eventEntity.Id, user2.Id));

        await using var verifyContext = _container.CreateContext();
        var verifyRepository = new BookingRepository(verifyContext);

        var user1Bookings = await verifyRepository.GetBookingsByUserId(user1.Id);

        Assert.Equal(2, user1Bookings.Count);
        Assert.All(user1Bookings, b => Assert.Equal(user1.Id, b.UserId));
    }

    [Fact]
    public async Task GetBookingsByUserId_IncludesEventAndUser()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context, "included_user");
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context, title: "Выставка");

        var repository = new BookingRepository(context);
        await repository.CreateBookingAsync(new Booking(eventEntity.Id, user.Id));

        await using var verifyContext = _container.CreateContext();
        var verifyRepository = new BookingRepository(verifyContext);

        var bookings = await verifyRepository.GetBookingsByUserId(user.Id);

        Assert.Single(bookings);
        Assert.NotNull(bookings[0].Event);
        Assert.Equal("Выставка", bookings[0].Event.Title);
        Assert.NotNull(bookings[0].User);
        Assert.Equal("included_user", bookings[0].User.Login);
    }

    [Fact]
    public async Task CountActiveBookingsByUserIdAsync_CountsPendingAndConfirmedOnly()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context);
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context, totalSeats: 10);

        var pending = new Booking(eventEntity.Id, user.Id);
        var confirmed = new Booking(eventEntity.Id, user.Id) { CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        confirmed.Confirm();
        var rejected = new Booking(eventEntity.Id, user.Id) { CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        rejected.Reject();
        var cancelled = new Booking(eventEntity.Id, user.Id) { CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        cancelled.Cancel();

        context.Bookings.AddRange(pending, confirmed, rejected, cancelled);
        await context.SaveChangesAsync();

        await using var verifyContext = _container.CreateContext();
        var repository = new BookingRepository(verifyContext);

        var count = await repository.CountActiveBookingsByUserIdAsync(user.Id);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CancelBookingAsync_ExistingBooking_SetsCancelledStatus()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context);
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context);

        var booking = new Booking(eventEntity.Id, user.Id) { CreatedAt = DateTime.UtcNow.AddMinutes(-1) };
        var repository = new BookingRepository(context);
        await repository.CreateBookingAsync(booking);

        await using var cancelContext = _container.CreateContext();
        var cancelRepository = new BookingRepository(cancelContext);
        await cancelRepository.CancelBookingAsync(booking.Id);

        await using var verifyContext = _container.CreateContext();
        var savedBooking = await verifyContext.Bookings.FindAsync(booking.Id);

        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Cancelled, savedBooking.Status);
        Assert.NotNull(savedBooking.ProcessedAt);
    }

    [Fact]
    public async Task CancelBookingAsync_NonExistingBooking_DoesNotThrow()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new BookingRepository(context);

        var exception = await Record.ExceptionAsync(() =>
            repository.CancelBookingAsync(Guid.NewGuid()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsChanges()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context);
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context);

        var booking = new Booking(eventEntity.Id, user.Id);
        context.Bookings.Add(booking);

        var repository = new BookingRepository(context);
        await repository.SaveChangesAsync();

        await using var verifyContext = _container.CreateContext();
        var savedBooking = await verifyContext.Bookings.FindAsync(booking.Id);

        Assert.NotNull(savedBooking);
    }

    [Fact]
    public async Task CreateBooking_ViaService_SavesSuccessfully()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context);
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context);

        var bookingRepository = new BookingRepository(context);
        var eventRepository = new EventRepository(context);
        var userRepository = new UserRepository(context);
        var service = new BookingService(bookingRepository, eventRepository, userRepository);

        await service.CreateBookingAsync(eventEntity.Id, user.Id);

        await using var verifyContext = _container.CreateContext();
        var booking = await verifyContext.Bookings.FirstAsync();

        Assert.NotNull(booking);
        Assert.Equal(eventEntity.Id, booking.EventId);
        Assert.Equal(user.Id, booking.UserId);
    }

    [Fact]
    public async Task CreateBooking_ConcurrentRequests_NoOverbooking()
    {
        await ResetDatabaseAsync();

        const int totalSeats = 5;

        await using var context = _container.CreateContext();
        var user = await IntegrationTestDataHelper.SeedUserAsync(context);
        var eventEntity = await IntegrationTestDataHelper.SeedEventAsync(context, totalSeats);

        var successfulBookings = 0;
        var failedBookings = 0;

        var tasks = Enumerable.Range(0, totalSeats * 3)
            .Select(_ => Task.Run(async () =>
            {
                await using var context = _container.CreateContext();
                var bookingRepository = new BookingRepository(context);
                var eventRepository = new EventRepository(context);
                var userRepository = new UserRepository(context);
                var bookingService = new BookingService(bookingRepository, eventRepository, userRepository);

                try
                {
                    await bookingService.CreateBookingAsync(eventEntity.Id, user.Id);
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
