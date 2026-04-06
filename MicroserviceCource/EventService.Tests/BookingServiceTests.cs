using MicroserviceCourse.Data;
using MicroserviceCourse.Model.Entity;
using MicroserviceCourse.Model.Enum;
using Microsoft.EntityFrameworkCore;

namespace EventService.Tests;

public class BookingServiceTests
{
    private AppDbContext _dbContext;
    private List<Event> _events;
    private MicroserviceCourse.Services.BookingService _bookingService;
    
    private static readonly Guid[] Guids =
        [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

    public BookingServiceTests()
    {
        _events =
        [
            new Event("крещение Руси", "988 год", DateTime.Now, DateTime.Now.AddDays(1))
            {
                Id = Guids[0],
            },

            new Event("битва на реке Калке", "1223 год", DateTime.Now, DateTime.Now.AddDays(1))
            {
                Id = Guids[1],
            },

            new Event("Отечественная война", "1812 год", DateTime.Now, DateTime.Now.AddDays(1))
            {
                Id = Guids[2],
            }

        ];
        
        SetupDbContext();
        
        _bookingService = new MicroserviceCourse.Services.BookingService(_dbContext);
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

    [Fact]
    public async Task UpdateStatus_Correct()
    {
        var eventId = Guids[0];
        const BookingStatus status = BookingStatus.Confirmed;
        var createdBooking = await _bookingService.CreateBookingAsync(eventId);
        
        await _bookingService.UpdateStatusAsync(createdBooking.Id, status);
        
        var updatedBooking = await _bookingService.GetBookingByIdAsync(createdBooking.Id);
        
        Assert.Equal(status, updatedBooking.Status);
    }
    
    [Fact]
    public async Task UpdateStatus_KeyNotFoundException()
    {
        var randomId = Guid.NewGuid();
        var status = BookingStatus.Confirmed;
        
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _bookingService.UpdateStatusAsync(randomId, status));
    }

    #endregion
}