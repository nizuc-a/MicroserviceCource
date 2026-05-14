using MicroserviceCourse.Data;
using MicroserviceCourse.Exceptions;
using MicroserviceCourse.Model.Entity;
using MicroserviceCourse.Model.Enum;
using MicroserviceCourse.Services;
using Microsoft.EntityFrameworkCore;

namespace EventService.Tests;

public class BookingServiceTests
{
    private AppDbContext _dbContext;
    private readonly List<Event> _events;
    private readonly BookingService _bookingService;

    private static readonly Guid[] Guids =
        [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

    public BookingServiceTests()
    {
        _events =
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

        SetupDbContext();

        _bookingService = new BookingService(_dbContext!);
    }


    private void SetupDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new AppDbContext(options);

        _dbContext.Events.AddRange(_events);
        _dbContext.SaveChanges();
    }

    #region Create Booking

    [Fact]
    public async Task CreateBooking_Correct()
    {
        var eventId = Guids[0];

        var booking = await _bookingService.CreateBookingAsync(eventId);

        Assert.Equal(booking.EventId, eventId);

        var eventEntity = await _dbContext.Events.FindAsync(eventId);

        Assert.Equal(eventEntity?.AvailableSeats, eventEntity?.TotalSeats - 1);
    }

    [Fact]
    public async Task CreateBooking_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _bookingService.CreateBookingAsync(randomId));
    }

    #endregion

    #region Get Booking By Id

    [Fact]
    public async Task GetBookingById_Correct()
    {
        var eventId = Guids[0];
        var createdBooking = await _bookingService.CreateBookingAsync(eventId);

        var booking = await _bookingService.GetBookingByIdAsync(createdBooking.Id);

        Assert.Equal(booking.Id, createdBooking.Id);
    }

    [Fact]
    public async Task GetBookingById_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _bookingService.GetBookingByIdAsync(randomId));
    }

    #endregion

    #region Update status

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public async Task UpdateStatus_Correct(BookingStatus status)
    {
        var eventId = Guids[0];
        var booking = await _bookingService.CreateBookingAsync(eventId);

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

        var createdBooking = await _bookingService.CreateBookingAsync(eventId);

        Assert.Equal(status, createdBooking.Status);
    }

    [Fact]
    public async Task UpdateStatus_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();
        var status = BookingStatus.Confirmed;

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _bookingService.UpdateStatusAsync(randomId, status));
    }

    #endregion

    #region Overbooking

    [Fact]
    public async Task CreateBooking_LimitBook_Correct()
    {
        var eventId = Guids[0];

        var eventEntity = await _dbContext.Events.FindAsync(eventId);

        var bookings = new List<Booking>();
        var availableSeats = eventEntity?.AvailableSeats;

        for (int i = 0; i < availableSeats; i++)
        {
            var booking = await _bookingService.CreateBookingAsync(eventId);
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

        var eventEntity = await _dbContext.Events.FindAsync(eventId);
        
        var availableSeats = eventEntity?.AvailableSeats;

        for (int i = 0; i < availableSeats; i++)
        {
            var booking = await _bookingService.CreateBookingAsync(eventId);

            Assert.Equal(booking.EventId, eventId);
        }

        await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            await _bookingService.CreateBookingAsync(eventId));
    }

    // Тест: один экземпляр контекста и сервиса для всех вызовов
    [Fact]
    public async Task CreateBooking_HighConcurrency()
    {
        var eventId = Guids[0];
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        // Наполнение БД
        await using (var seedContext = new AppDbContext(options))
        {
            seedContext.Events.AddRange(_events);
            await seedContext.SaveChangesAsync();
        }

        // Один контекст и один сервис — общие для всех потоков
        await using var sharedContext = new AppDbContext(options);
        var sharedService = new BookingService(sharedContext);

        var eventEntity = await sharedContext.Events.FindAsync(eventId);
        var availableSeats = eventEntity?.AvailableSeats;
        var totalAttempts = availableSeats * 3;
        
        var successfulBookings = 0;
        var failedBookings = 0;

        var tasks = new List<Task>();
        for (int i = 0; i < totalAttempts; i++)
        {
            Thread.Sleep(10);
            tasks.Add(Task.Run( async () => 
            {
                try
                {
                    await sharedService.CreateBookingAsync(eventId);
                    Interlocked.Increment(ref successfulBookings);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref failedBookings);
                }
                
            }));
        }

        await Task.WhenAll(tasks);
        
        Assert.Equal(availableSeats, successfulBookings);
        Assert.Equal(totalAttempts - availableSeats, failedBookings);
    }

    #endregion
}