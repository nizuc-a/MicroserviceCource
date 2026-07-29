using EventService.Application.Abstractions.Repositories;
using EventService.Domain.Entities;
using Moq;

namespace EventService.UnitTests;

public class EventServiceCacheInvalidationTests
{
    [Fact]
    public async Task BookEvent_InvalidatesCacheAfterSuccessfulBooking()
    {
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entity = new Event("concert", "desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10)
        {
            Id = eventId
        };

        var repo = new Mock<IEventRepository>();
        repo.Setup(r => r.GetTrackedByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        repo.Setup(r => r.AddOutboxMessageAsync(It.IsAny<Shared.Domain.Entities.OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.InvalidateCacheAsync(eventId))
            .Returns(Task.CompletedTask);

        var service = new Application.Services.EventService(repo.Object);

        await service.BookEvent(eventId, bookingId, userId, seatCount: 1);

        repo.Verify(r => r.InvalidateCacheAsync(eventId), Times.Once);
        Assert.Equal(9, entity.AvailableSeats);
    }

    [Fact]
    public async Task ReleaseBookingAsync_InvalidatesCacheAfterSave()
    {
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var entity = new Event("concert", "desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10)
        {
            Id = eventId,
            AvailableSeats = 8
        };
        entity.AddBooking(bookingId);

        var repo = new Mock<IEventRepository>();
        repo.Setup(r => r.GetTrackedByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.InvalidateCacheAsync(eventId))
            .Returns(Task.CompletedTask);

        var service = new Application.Services.EventService(repo.Object);

        await service.ReleaseBookingAsync(eventId, bookingId, seatCount: 2);

        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.InvalidateCacheAsync(eventId), Times.Once);
        Assert.Equal(10, entity.AvailableSeats);
    }
}
