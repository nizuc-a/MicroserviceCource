using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Infrastructure.Repository;
using BookingService.IntegrationTests.DatabaseFixtures;
using AppBookingService = BookingService.Application.Services.BookingService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Domain.Contracts.Booking;
using Shared.Domain.Kafka;
using Shared.Domain.Settings;
using Xunit;

namespace BookingService.IntegrationTests;

[Collection("Database")]
public class BookingRepositoryTests
{
    private readonly PostgreSqlContainerFixture _container;

    public BookingRepositoryTests(PostgreSqlContainerFixture container)
    {
        _container = container;
    }

    private async Task ResetDatabaseAsync() => await _container.ResetDatabaseAsync();

    [Fact]
    public async Task CreateBookingAsync_WithValidData_SavesSuccessfully()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new BookingRepository(context);
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var outbox = IntegrationTestDataHelper.CreateBookingCreatedOutbox(booking);

        await repository.CreateBookingAsync(booking, outbox);

        await using var verifyContext = _container.CreateContext();
        var savedBooking = await verifyContext.Bookings.FindAsync(booking.Id);

        Assert.NotNull(savedBooking);
        Assert.Equal(booking.EventId, savedBooking.EventId);
        Assert.Equal(booking.UserId, savedBooking.UserId);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);

        var savedOutbox = await verifyContext.OutboxMessages.FirstOrDefaultAsync(m => m.Key == booking.Id.ToString());
        Assert.NotNull(savedOutbox);
        Assert.Equal(nameof(BookingCreated), savedOutbox.Type);
    }

    [Fact]
    public async Task CreateBookingAsync_DuplicatedId_ThrowsDbUpdateException()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var bookingId = Guid.NewGuid();
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid()) { Id = bookingId };
        var outbox = IntegrationTestDataHelper.CreateBookingCreatedOutbox(booking);

        var repository = new BookingRepository(context);
        await repository.CreateBookingAsync(booking, outbox);

        await using var verifyContext = _container.CreateContext();
        var duplicateBooking = new Booking(Guid.NewGuid(), Guid.NewGuid()) { Id = bookingId };
        var duplicateOutbox = IntegrationTestDataHelper.CreateBookingCreatedOutbox(duplicateBooking);
        var verifyRepository = new BookingRepository(verifyContext);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            verifyRepository.CreateBookingAsync(duplicateBooking, duplicateOutbox));
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingBooking_ReturnsBooking()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var repository = new BookingRepository(context);
        await repository.CreateBookingAsync(booking, IntegrationTestDataHelper.CreateBookingCreatedOutbox(booking));

        await using var verifyContext = _container.CreateContext();
        var verifyRepository = new BookingRepository(verifyContext);

        var result = await verifyRepository.GetBookingByIdAsync(booking.Id);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.EventId, result.EventId);
        Assert.Equal(booking.UserId, result.UserId);
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
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var repository = new BookingRepository(context);

        var booking1 = new Booking(eventId, user1Id);
        var booking2 = new Booking(eventId, user1Id);
        var booking3 = new Booking(eventId, user2Id);

        await repository.CreateBookingAsync(booking1, IntegrationTestDataHelper.CreateBookingCreatedOutbox(booking1));
        await repository.CreateBookingAsync(booking2, IntegrationTestDataHelper.CreateBookingCreatedOutbox(booking2));
        await repository.CreateBookingAsync(booking3, IntegrationTestDataHelper.CreateBookingCreatedOutbox(booking3));

        await using var verifyContext = _container.CreateContext();
        var verifyRepository = new BookingRepository(verifyContext);

        var user1Bookings = await verifyRepository.GetBookingsByUserId(user1Id);

        Assert.Equal(2, user1Bookings.Count);
        Assert.All(user1Bookings, b => Assert.Equal(user1Id, b.UserId));
    }

    [Fact]
    public async Task CountActiveBookingsByUserIdAsync_CountsPendingAndConfirmedOnly()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var pending = new Booking(eventId, userId);
        var confirmed = new Booking(eventId, userId) { CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        confirmed.Confirm();
        var rejected = new Booking(eventId, userId) { CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        rejected.Reject();
        var cancelled = new Booking(eventId, userId) { CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        cancelled.Cancel();

        context.Bookings.AddRange(pending, confirmed, rejected, cancelled);
        await context.SaveChangesAsync();

        await using var verifyContext = _container.CreateContext();
        var repository = new BookingRepository(verifyContext);

        var count = await repository.CountActiveBookingsByUserIdAsync(userId);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetActiveBookingsByEventIdAsync_ReturnsPendingAndConfirmedOnly()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var pending = new Booking(eventId, userId);
        var confirmed = new Booking(eventId, userId) { CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        confirmed.Confirm();
        var rejected = new Booking(eventId, userId) { CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        rejected.Reject();

        context.Bookings.AddRange(pending, confirmed, rejected);
        await context.SaveChangesAsync();

        await using var verifyContext = _container.CreateContext();
        var repository = new BookingRepository(verifyContext);

        var activeBookings = await repository.GetActiveBookingsByEventIdAsync(eventId);

        Assert.Equal(2, activeBookings.Count);
    }

    [Fact]
    public async Task CancelBookingAsync_ExistingBooking_SetsCancelledStatusAndCreatesOutbox()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid()) { CreatedAt = DateTime.UtcNow.AddMinutes(-1) };
        var repository = new BookingRepository(context);
        await repository.CreateBookingAsync(booking, IntegrationTestDataHelper.CreateBookingCreatedOutbox(booking));

        await using var cancelContext = _container.CreateContext();
        var cancelRepository = new BookingRepository(cancelContext);
        await cancelRepository.CancelBookingAsync(booking.Id, IntegrationTestDataHelper.CreateBookingCancelledOutbox(booking));

        await using var verifyContext = _container.CreateContext();
        var savedBooking = await verifyContext.Bookings.FindAsync(booking.Id);

        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Cancelled, savedBooking.Status);
        Assert.NotNull(savedBooking.ProcessedAt);

        var outbox = await verifyContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Type == nameof(BookingCancelled) && m.Key == booking.Id.ToString());
        Assert.NotNull(outbox);
    }

    [Fact]
    public async Task CancelBookingAsync_NonExistingBooking_DoesNotThrow()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new BookingRepository(context);
        var outbox = new Shared.Domain.Entities.OutboxMessage
        {
            Topic = KafkaTopics.Bookings,
            Key = Guid.NewGuid().ToString(),
            Type = nameof(BookingCancelled),
            Payload = "{}"
        };

        var exception = await Record.ExceptionAsync(() =>
            repository.CancelBookingAsync(Guid.NewGuid(), outbox));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsChanges()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
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
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userOptions = Options.Create(new UserSettings { MaxActiveBookingsPerUser = 10 });

        var bookingRepository = new BookingRepository(context);
        var service = new AppBookingService(bookingRepository, userOptions);

        await service.CreateBookingAsync(eventId, userId);

        await using var verifyContext = _container.CreateContext();
        var booking = await verifyContext.Bookings.FirstAsync();

        Assert.NotNull(booking);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(userId, booking.UserId);

        var outbox = await verifyContext.OutboxMessages.FirstAsync();
        Assert.Equal(nameof(BookingCreated), outbox.Type);
    }

    [Fact]
    public async Task CreateBooking_ConcurrentRequests_RespectsActiveBookingLimit()
    {
        await ResetDatabaseAsync();

        const int maxActiveBookings = 5;
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userOptions = Options.Create(new UserSettings { MaxActiveBookingsPerUser = maxActiveBookings });

        var successfulBookings = 0;
        var failedBookings = 0;

        var tasks = Enumerable.Range(0, maxActiveBookings * 3)
            .Select(_ => Task.Run(async () =>
            {
                await using var context = _container.CreateContext();
                var bookingRepository = new BookingRepository(context);
                var bookingService = new AppBookingService(bookingRepository, userOptions);

                try
                {
                    await bookingService.CreateBookingAsync(eventId, userId);
                    Interlocked.Increment(ref successfulBookings);
                }
                catch (BookingService.Domain.Exceptions.ActiveBookingLimitExceededException)
                {
                    Interlocked.Increment(ref failedBookings);
                }
            }));

        await Task.WhenAll(tasks);

        Assert.Equal(maxActiveBookings, successfulBookings);
        Assert.Equal(maxActiveBookings * 2, failedBookings);
    }
}
