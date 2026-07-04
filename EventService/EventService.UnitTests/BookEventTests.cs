using EventService.Domain.Entities;
using EventService.Infrastructure.DbContext;
using EventService.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Contracts.Booking;
using Shared.Domain.Entities;

namespace EventService.UnitTests;

public class BookEventTests
{
    private AppDbContext _dbContext = null!;
    private Application.Services.EventService _eventService = null!;
    private Guid _eventId;

    public BookEventTests()
    {
        SetupDbContext();
        _eventService = new Application.Services.EventService(new EventRepository(_dbContext));
    }

    private void SetupDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new AppDbContext(options);

        var eventEntity = new Event(
            "Концерт",
            "Описание",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            5);

        _eventId = eventEntity.Id;
        _dbContext.Events.Add(eventEntity);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task BookEvent_WithAvailableSeats_ReservesSeatAndCreatesOutboxMessage()
    {
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _eventService.BookEvent(_eventId, bookingId, userId);

        var eventEntity = await _dbContext.Events.FindAsync(_eventId);
        Assert.NotNull(eventEntity);
        Assert.Equal(4, eventEntity.AvailableSeats);
        Assert.Contains(bookingId, eventEntity.BookingIds);

        var outbox = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Key == bookingId.ToString());
        Assert.NotNull(outbox);
        Assert.Equal(nameof(BookingConfirmed), outbox.Type);
    }

    [Fact]
    public async Task BookEvent_NoAvailableSeats_CreatesRejectedOutboxMessage()
    {
        var eventEntity = await _dbContext.Events.FindAsync(_eventId);
        eventEntity!.AvailableSeats = 0;
        await _dbContext.SaveChangesAsync();

        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _eventService.BookEvent(_eventId, bookingId, userId);

        var outbox = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Key == bookingId.ToString());
        Assert.NotNull(outbox);
        Assert.Equal(nameof(BookingRejected), outbox.Type);
    }

    [Fact]
    public async Task BookEvent_NonExistingEvent_CreatesRejectedOutboxMessage()
    {
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _eventService.BookEvent(eventId, bookingId, userId);

        var outbox = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Key == bookingId.ToString());
        Assert.NotNull(outbox);
        Assert.Equal(nameof(BookingRejected), outbox.Type);
    }
}
