using EventService.Api.Data;
using EventService.Api.Interfaces.Repository;
using EventService.Api.Interfaces.Services;
using EventService.Api.Repository;
using EventService.Api.Services;
using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.UnitTests;

public class BookingServiceTests
{
    private readonly IServiceProvider _serviceProvider;

    private static readonly Guid[] Guids =
        [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingService,BookingService>();

        _serviceProvider = services.BuildServiceProvider();
        
        List<Event> events =
        [
            new Event("крещение Руси", "988 год", DateTime.Now, DateTime.Now.AddDays(1), 10)
            {
                Id = Guids[0],
            },

            new Event("битва на реке Калке", "1223 год", DateTime.Now, DateTime.Now.AddDays(1), 10)
            {
                Id = Guids[1],
            },

            new Event("Отечественная война", "1812 год", DateTime.Now, DateTime.Now.AddDays(1), 10)
            {
                Id = Guids[2],
            }
        ];
        
        SetupDbContext(events);
    }
    
    private void SetupDbContext(List<Event> events)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Events.AddRange(events);
        dbContext.SaveChanges();
    }

    #region Create Booking

    [Fact]
    public async Task CreateBooking_Correct()
    {
        var eventId = Guids[0];

        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var booking = await bookingService.CreateBookingAsync(eventId);

        Assert.Equal(booking.EventId, eventId);

        var eventEntity = await dbContext.Events.FindAsync(eventId);

        Assert.Equal(eventEntity?.AvailableSeats, eventEntity?.TotalSeats - 1);
    }

    [Fact]
    public async Task CreateBooking_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await bookingService.CreateBookingAsync(randomId));
    }

    #endregion

    #region Get Booking By Id

    [Fact]
    public async Task GetBookingById_Correct()
    {
        var eventId = Guids[0];
        
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        var createdBooking = await bookingService.CreateBookingAsync(eventId);

        var booking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        Assert.Equal(booking.Id, createdBooking.Id);
    }

    [Fact]
    public async Task GetBookingById_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();
        
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await bookingService.GetBookingByIdAsync(randomId));
    }

    #endregion

    #region Update status

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public async Task UpdateStatus_Correct(BookingStatus status)
    {
        var eventId = Guids[0];
        
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        
        var booking = await bookingService.CreateBookingAsync(eventId);

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
        var eventId = Guids[0];
        const BookingStatus status = BookingStatus.Pending;
        
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var createdBooking = await bookingService.CreateBookingAsync(eventId);

        Assert.Equal(status, createdBooking.Status);
    }

    #endregion

    #region Overbooking

    [Fact]
    public async Task CreateBooking_LimitBook_Correct()
    {
        var eventId = Guids[0];
        
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventEntity = await dbContext.Events.FindAsync(eventId);

        var bookings = new List<Booking>();
        var availableSeats = eventEntity?.AvailableSeats;

        for (int i = 0; i < availableSeats; i++)
        {
            var booking = await bookingService.CreateBookingAsync(eventId);
            bookings.Add(booking);

            Assert.Equal(booking.EventId, eventId);
        }

        var uniqueBookingCount = bookings.Select(x => x.Id).Distinct().Count();

        Assert.Equal(uniqueBookingCount, bookings.Count);
        Assert.Equal(0, eventEntity?.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_LimitBook_NoAvailableSeatsException()
    {
        var eventId = Guids[0];
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var eventEntity = await dbContext.Events.FindAsync(eventId);
        
        var availableSeats = eventEntity?.AvailableSeats;

        for (int i = 0; i < availableSeats; i++)
        {
            var booking = await bookingService.CreateBookingAsync(eventId);

            Assert.Equal(booking.EventId, eventId);
        }

        await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            await bookingService.CreateBookingAsync(eventId));
    }
    
    [Fact]
    public async Task CreateBooking_HighConcurrency()
    {
        var eventId = Guids[0];
        
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
                    await bookingService.CreateBookingAsync(eventId);
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