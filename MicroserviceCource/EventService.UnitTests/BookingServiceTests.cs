using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Application.Services;
using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Domain.Exceptions;
using EventService.Infrastructure;
using EventService.Infrastructure.DbContext;
using EventService.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.UnitTests;

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

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();
        
        List<Event> events =
        [
            new Event("крещение Руси", "988 год", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 10)
            {
                Id = EventGuids[0],
            },

            new Event("битва на реке Калке", "1223 год", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 10)
            {
                Id = EventGuids[1],
            },

            new Event("Отечественная война", "1812 год", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 10)
            {
                Id = EventGuids[2],
            }
        ];

        List<User> users =
        [
            new User("user1", "hash1")
            {
                Id = UserGuids[0],
            },

            new User("user2", "hash2")
            {
                Id = UserGuids[1],
            }
        ];
        
        SetupDbContext(events, users);
    }
    
    private void SetupDbContext(List<Event> events, List<User> users)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Users.AddRange(users);
        dbContext.Events.AddRange(events);
        dbContext.SaveChanges();
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

        var eventEntity = await dbContext.Events.FindAsync(eventId);

        Assert.Equal(eventEntity?.AvailableSeats, eventEntity?.TotalSeats - 1);
    }

    [Fact]
    public async Task CreateBooking_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await bookingService.CreateBookingAsync(randomId, userId));
    }

    [Fact]
    public async Task CreateBooking_UserNotFoundException()
    {
        var eventId = EventGuids[0];
        var randomUserId = Guid.NewGuid();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<UserNotFoundException>(async () =>
            await bookingService.CreateBookingAsync(eventId, randomUserId));
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

        await bookingService.CancelBookingAsync(booking.Id);

        var cancelledBooking = await dbContext.Bookings.FindAsync(booking.Id);

        Assert.NotNull(cancelledBooking);
        Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);
        Assert.NotNull(cancelledBooking.ProcessedAt);
    }

    [Fact]
    public async Task CancelBooking_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await bookingService.CancelBookingAsync(randomId));
    }

    [Fact]
    public async Task CancelBooking_AlreadyCancelled_BookingAlreadyCancelledException()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var booking = await bookingService.CreateBookingAsync(eventId, userId);
        await bookingService.CancelBookingAsync(booking.Id);

        await Assert.ThrowsAsync<BookingAlreadyCancelledException>(async () =>
            await bookingService.CancelBookingAsync(booking.Id));
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

    #region Overbooking

    [Fact]
    public async Task CreateBooking_LimitBook_Correct()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];
        
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventEntity = await dbContext.Events.FindAsync(eventId);

        var bookings = new List<Booking>();
        var availableSeats = eventEntity?.AvailableSeats;

        for (int i = 0; i < availableSeats; i++)
        {
            var booking = await bookingService.CreateBookingAsync(eventId, userId);
            bookings.Add(booking);

            Assert.Equal(eventId, booking.EventId);
            Assert.Equal(userId, booking.UserId);
        }

        var uniqueBookingCount = bookings.Select(x => x.Id).Distinct().Count();

        Assert.Equal(uniqueBookingCount, bookings.Count);
        Assert.Equal(0, eventEntity?.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_LimitBook_NoAvailableSeatsException()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var eventEntity = await dbContext.Events.FindAsync(eventId);
        
        var availableSeats = eventEntity?.AvailableSeats;

        for (int i = 0; i < availableSeats; i++)
        {
            var booking = await bookingService.CreateBookingAsync(eventId, userId);

            Assert.Equal(eventId, booking.EventId);
            Assert.Equal(userId, booking.UserId);
        }

        await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            await bookingService.CreateBookingAsync(eventId, userId));
    }
    
    [Fact]
    public async Task CreateBooking_HighConcurrency()
    {
        var eventId = EventGuids[0];
        var userId = UserGuids[0];
        
        using var dbScope = _serviceProvider.CreateScope();
        var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventEntity = await dbContext.Events.FindAsync(eventId);
        var availableSeats = eventEntity?.AvailableSeats ?? 0;
        var totalAttempts = availableSeats * 3;
        
        var successfulBookings = 0;
        var failedBookings = 0;
        
        var tasks = Enumerable.Range(0, totalAttempts)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    await bookingService.CreateBookingAsync(eventId, userId);
                    Interlocked.Increment(ref successfulBookings);
                }
                catch (NoAvailableSeatsException)
                {
                    Interlocked.Increment(ref failedBookings);
                }
            })); 

        await Task.WhenAll(tasks);
        
        Assert.Equal(availableSeats, successfulBookings);
        Assert.Equal(totalAttempts - availableSeats, failedBookings);
    }

    #endregion
}
