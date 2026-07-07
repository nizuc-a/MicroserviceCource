using BookingService.Application.Abstractions.Repository;
using BookingService.Application.Abstractions.Services;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;
using BookingService.Infrastructure.DbContext;
using BookingService.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Contracts.Booking;
using Shared.Domain.Settings;

namespace BookingService.UnitTests;

public class BookingServiceTests
{
    private readonly IServiceProvider _serviceProvider;

    private static readonly Guid[] EventGuids =
        [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

    private static readonly Guid[] UserGuids =
        [Guid.NewGuid(), Guid.NewGuid()];

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.Configure<UserSettings>(options => options.MaxActiveBookingsPerUser = 10);
        services.AddScoped<IBookingService, Application.Services.BookingService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    #region Create Booking

    [Fact]
    public async Task CreateBooking_Correct()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await bookingService.CreateBookingAsync(eventId, userId);

        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(userId, booking.UserId);
        Assert.Equal(BookingStatus.Pending, booking.Status);

        var outbox = await dbContext.OutboxMessages.FirstOrDefaultAsync(m => m.Key == booking.Id.ToString());
        Assert.NotNull(outbox);
        Assert.Equal(nameof(BookingCreated), outbox.Type);
    }

    [Fact]
    public async Task CreateBooking_WithSeatCount_PublishesPayloadWithSeatCount()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];
        const int seatCount = 3;

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await bookingService.CreateBookingAsync(eventId, userId, seatCount);

        Assert.Equal(seatCount, booking.SeatCount);

        var outbox = await dbContext.OutboxMessages.FirstOrDefaultAsync(m => m.Key == booking.Id.ToString());
        Assert.NotNull(outbox);

        var payload = System.Text.Json.JsonSerializer.Deserialize<BookingCreated>(outbox.Payload);
        Assert.NotNull(payload);
        Assert.Equal(seatCount, payload.SeatCount);
        Assert.True(payload.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateBooking_InvalidSeatCount_Throws()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            bookingService.CreateBookingAsync(EventGuids[0], UserGuids[0], seatCount: 0));
    }

    [Fact]
    public async Task CreateBooking_ActiveBookingLimitExceededException()
    {
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        for (var i = 0; i < 10; i++)
            await bookingService.CreateBookingAsync(EventGuids[i % EventGuids.Length], userId);

        await Assert.ThrowsAsync<ActiveBookingLimitExceededException>(async () =>
            await bookingService.CreateBookingAsync(EventGuids[0], userId));
    }

    [Fact]
    public async Task CreateBooking_AfterCancelledBooking_AllowsNewBooking()
    {
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        for (var i = 0; i < 10; i++)
            await bookingService.CreateBookingAsync(EventGuids[i % EventGuids.Length], userId);

        var bookings = await bookingService.GetBookingsByUserId(userId);
        await bookingService.CancelBookingAsync(bookings[0].Id, userId, isAdmin: false);

        var newBooking = await bookingService.CreateBookingAsync(EventGuids[0], userId);

        Assert.Equal(userId, newBooking.UserId);
    }

    [Fact]
    public async Task CreateBooking_ActiveBookingLimit_DoesNotAffectOtherUsers()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        for (var i = 0; i < 10; i++)
            await bookingService.CreateBookingAsync(EventGuids[i % EventGuids.Length], UserGuids[0]);

        await Assert.ThrowsAsync<ActiveBookingLimitExceededException>(async () =>
            await bookingService.CreateBookingAsync(EventGuids[0], UserGuids[0]));

        var booking = await bookingService.CreateBookingAsync(EventGuids[0], UserGuids[1]);

        Assert.Equal(UserGuids[1], booking.UserId);
    }

    #endregion

    #region Get Booking By Id

    [Fact]
    public async Task GetBookingById_Correct()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var createdBooking = await bookingService.CreateBookingAsync(eventId, userId);

        var booking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        Assert.Equal(createdBooking.Id, booking.Id);
        Assert.Equal(userId, booking.UserId);
    }

    [Fact]
    public async Task GetBookingById_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await bookingService.GetBookingByIdAsync(randomId));
    }

    #endregion

    #region Get Bookings By User Id

    [Fact]
    public async Task GetBookingsByUserId_Correct()
    {
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var firstBooking = await bookingService.CreateBookingAsync(EventGuids[0], userId);
        var secondBooking = await bookingService.CreateBookingAsync(EventGuids[1], userId);

        var bookings = await bookingService.GetBookingsByUserId(userId);

        Assert.Equal(2, bookings.Count);
        Assert.Contains(bookings, x => x.Id == firstBooking.Id && x.EventId == EventGuids[0]);
        Assert.Contains(bookings, x => x.Id == secondBooking.Id && x.EventId == EventGuids[1]);
        Assert.All(bookings, x => Assert.Equal(userId, x.UserId));
    }

    [Fact]
    public async Task GetBookingsByUserId_ReturnsOnlyUserBookings()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await bookingService.CreateBookingAsync(EventGuids[0], UserGuids[0]);
        await bookingService.CreateBookingAsync(EventGuids[1], UserGuids[1]);

        var userBookings = await bookingService.GetBookingsByUserId(UserGuids[0]);

        Assert.Single(userBookings);
        Assert.Equal(UserGuids[0], userBookings[0].UserId);
    }

    [Fact]
    public async Task GetBookingsByUserId_EmptyList()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var bookings = await bookingService.GetBookingsByUserId(UserGuids[0]);

        Assert.Empty(bookings);
    }

    #endregion

    #region Cancel Booking

    [Fact]
    public async Task CancelBooking_Correct()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await bookingService.CreateBookingAsync(eventId, userId);

        await bookingService.CancelBookingAsync(booking.Id, userId, isAdmin: false);

        var cancelledBooking = await dbContext.Bookings.FindAsync(booking.Id);

        Assert.NotNull(cancelledBooking);
        Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);
        Assert.NotNull(cancelledBooking.ProcessedAt);

        var outbox = await dbContext.OutboxMessages
            .Where(m => m.Type == nameof(BookingCancelled))
            .FirstOrDefaultAsync(m => m.Key == booking.Id.ToString());
        Assert.NotNull(outbox);
    }

    [Fact]
    public async Task CancelBooking_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await bookingService.CancelBookingAsync(randomId, userId, isAdmin: false));
    }

    [Fact]
    public async Task CancelBooking_AlreadyCancelled_BookingAlreadyCancelledException()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var booking = await bookingService.CreateBookingAsync(eventId, userId);
        await bookingService.CancelBookingAsync(booking.Id, userId, isAdmin: false);

        await Assert.ThrowsAsync<BookingAlreadyCancelledException>(async () =>
            await bookingService.CancelBookingAsync(booking.Id, userId, isAdmin: false));
    }

    [Fact]
    public async Task CancelBooking_OtherUserBooking_PermissionDeniedException()
    {
        var eventId = EventGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var booking = await bookingService.CreateBookingAsync(eventId, UserGuids[0]);

        await Assert.ThrowsAsync<PermissionDeniedException>(async () =>
            await bookingService.CancelBookingAsync(booking.Id, UserGuids[1], isAdmin: false));
    }

    [Fact]
    public async Task CancelBooking_AdminCanCancelOtherUserBooking()
    {
        var eventId = EventGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await bookingService.CreateBookingAsync(eventId, UserGuids[0]);

        await bookingService.CancelBookingAsync(booking.Id, UserGuids[1], isAdmin: true);

        var cancelledBooking = await dbContext.Bookings.FindAsync(booking.Id);

        Assert.NotNull(cancelledBooking);
        Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);
    }

    #endregion

    #region Update status

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public async Task UpdateStatus_Correct(BookingStatus status)
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var booking = await bookingService.CreateBookingAsync(eventId, userId);

        switch (status)
        {
            case BookingStatus.Confirmed:
                booking.Confirm();
                break;
            case BookingStatus.Rejected:
                booking.Reject();
                break;
        }

        Assert.Equal(status, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task UpdateStatus_CreatedStatus_Correct()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];
        const BookingStatus status = BookingStatus.Pending;

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var createdBooking = await bookingService.CreateBookingAsync(eventId, userId);

        Assert.Equal(status, createdBooking.Status);
        Assert.Equal(userId, createdBooking.UserId);
    }

    #endregion

    #region Concurrency

    [Fact]
    public async Task CreateBooking_ConcurrentRequests_RespectsActiveBookingLimit()
    {
        var userId = UserGuids[0];
        const int maxActiveBookings = 10;
        const int totalAttempts = 30;

        var successfulBookings = 0;
        var failedBookings = 0;

        var tasks = Enumerable.Range(0, totalAttempts)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    await bookingService.CreateBookingAsync(EventGuids[0], userId);
                    Interlocked.Increment(ref successfulBookings);
                }
                catch (ActiveBookingLimitExceededException)
                {
                    Interlocked.Increment(ref failedBookings);
                }
            }));

        await Task.WhenAll(tasks);

        Assert.Equal(maxActiveBookings, successfulBookings);
        Assert.Equal(totalAttempts - maxActiveBookings, failedBookings);
    }

    #endregion
}
