using EventService.Api.Data;
using EventService.Api.Repository;
using EventService.Domain.Entities;
using EventService.IntegrationTests.DatabaseFixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventService.IntegrationTests;

[Collection("Database")]
public class EventRepositoryTest 
{
    private readonly PostgreSqlContainerFixture _container;

    public EventRepositoryTest(PostgreSqlContainerFixture container)
    {
        _container = container;
    }

    private async Task ResetDatabaseAsync() => await _container.ResetDatabaseAsync();
    
    private AppDbContext CreateContext() => _container.CreateContext();

    [Fact]
    public async Task AddEvent_WithValidData_SavesSuccessfully()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var validEvent = new Event(
            "Корректное событие",
            "Нормальное описание",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10);

        // Act
        await repository.AddEventAsync(validEvent);

        // Assert
        await using var verifyContext = CreateContext();
        var savedEvent = await verifyContext.Events.FindAsync(validEvent.Id);
        Assert.NotNull(savedEvent);
        Assert.Equal("Корректное событие", savedEvent.Title);
        Assert.Equal("Нормальное описание", savedEvent.Description);
    }

    [Fact]
    public async Task AddEvent_WithInvalidData_ThrowsDbUpdateException()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var invalidEvent1 = new Event(
            "Некорректное событие",
            "Описание",
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(1),
            10);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddEventAsync(invalidEvent1));

        var longTitle = new string('A', 257);
        var invalidEvent2 = new Event(
            longTitle,
            "Описание",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddEventAsync(invalidEvent2));

        var longDescription = new string('B', 2001);
        var invalidEvent3 = new Event(
            "Нормальный заголовок",
            longDescription,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddEventAsync(invalidEvent3));
    }

    [Fact]
    public async Task CreateEvent_Exception_DuplicatedId()
    {
        await ResetDatabaseAsync();

        var id = Guid.NewGuid();
        await using var context = CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10)
        {
            Id = id
        };

        var repository = new EventRepository(context);
        await repository.AddEventAsync(eventEntity);

        await using var verifyContext = CreateContext();
        var verifyRepository = new EventRepository(verifyContext);

        var verifyEntity = new Event("Тест2", "Описание2", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10)
        {
            Id = id
        };

        await Assert.ThrowsAsync<DbUpdateException>(() => verifyRepository.AddEventAsync(verifyEntity));
    }

    [Fact]
    public async Task UpdateEvent()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);

        var repository = new EventRepository(context);
        await repository.AddEventAsync(eventEntity);

        await using var verifyContext = CreateContext();
        var verifyRepository = new EventRepository(verifyContext);

        eventEntity.Title = "Тест2";
        eventEntity.Description = "Описание2";

        await verifyRepository.UpdateEvent(eventEntity);

        await using var verifyContext2 = CreateContext();
        var verifyRepository2 = new EventRepository(verifyContext2);

        var result = await verifyRepository2.GetByIdAsync(eventEntity.Id);

        Assert.NotNull(result);
        Assert.Equal(eventEntity.Title, result.Title);
        Assert.Equal(eventEntity.Description, result.Description);
    }

    [Fact]
    public async Task DeleteEvent()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var eventEntity = new Event("Тест", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);

        var repository = new EventRepository(context);
        await repository.AddEventAsync(eventEntity);

        await using var verifyContext = CreateContext();
        var verifyRepository = new EventRepository(verifyContext);

        await verifyRepository.DeleteEventByIdAsync(eventEntity.Id);

        await using var verifyContext2 = CreateContext();
        var verifyRepository2 = new EventRepository(verifyContext2);

        var result = await verifyRepository2.GetByIdAsync(eventEntity.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAll_WithoutFilters_ReturnsAllEvents()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var event1 = new Event("Событие 1", "Описание 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        var event2 = new Event("Событие 2", "Описание 2", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 20);

        await repository.AddEventAsync(event1);
        await repository.AddEventAsync(event2);

        // Act
        var (events, totalCount) = await repository.GetAll();

        // Assert
        Assert.Equal(2, totalCount);
        Assert.Equal(2, events.Length);
    }

    [Fact]
    public async Task GetAll_WithTitleFilter_ReturnsMatchingEvents()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var event1 = new Event("Концерт", "Описание", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        var event2 = new Event("Выставка", "Описание", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 20);

        await repository.AddEventAsync(event1);
        await repository.AddEventAsync(event2);

        // Act
        var (events, totalCount) = await repository.GetAll(title: "концерт");

        // Assert
        Assert.Single(events);
        Assert.Equal("Концерт", events[0].Title);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task GetAll_WithFromDateFilter_ReturnsEventsAfterDate()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var baseDate = DateTime.UtcNow;
        var event1 = new Event("Событие 1", "", baseDate.AddDays(1), baseDate.AddDays(2), 10);
        var event2 = new Event("Событие 2", "", baseDate.AddDays(3), baseDate.AddDays(4), 20);

        await repository.AddEventAsync(event1);
        await repository.AddEventAsync(event2);

        // Act
        var (events, totalCount) = await repository.GetAll(from: baseDate.AddDays(2));

        // Assert
        Assert.Single(events);
        Assert.Equal("Событие 2", events[0].Title);
    }

    [Fact]
    public async Task GetAll_WithToDateFilter_ReturnsEventsBeforeDate()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var baseDate = DateTime.UtcNow;
        var event1 = new Event("Событие 1", "", baseDate.AddDays(1), baseDate.AddDays(2), 10);
        var event2 = new Event("Событие 2", "", baseDate.AddDays(3), baseDate.AddDays(4), 20);

        await repository.AddEventAsync(event1);
        await repository.AddEventAsync(event2);

        // Act
        var (events, totalCount) = await repository.GetAll(to: baseDate.AddDays(3));

        // Assert
        Assert.Single(events);
        Assert.Equal("Событие 1", events[0].Title);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsCorrectPage()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        for (int i = 1; i <= 15; i++)
        {
            var eventEntity = new Event($"Событие {i}", "", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
            await repository.AddEventAsync(eventEntity);
        }

        // Act
        var (events, totalCount) = await repository.GetAll(page: 2, pageSize: 5);

        // Assert
        Assert.Equal(15, totalCount);
        Assert.Equal(5, events.Length);
    }

    [Fact]
    public async Task GetAll_WithAllFilters_ReturnsFilteredAndPaginated()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var baseDate = DateTime.UtcNow;
        var event1 = new Event("Концерт Рок", "", baseDate.AddDays(1), baseDate.AddDays(2), 10);
        var event2 = new Event("Концерт Джаз", "", baseDate.AddDays(3), baseDate.AddDays(4), 20);
        var event3 = new Event("Выставка", "", baseDate.AddDays(5), baseDate.AddDays(6), 30);

        await repository.AddEventAsync(event1);
        await repository.AddEventAsync(event2);
        await repository.AddEventAsync(event3);

        // Act
        var (events, totalCount) = await repository.GetAll(
            title: "концерт",
            from: baseDate.AddDays(2),
            to: baseDate.AddDays(5),
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(events);
        Assert.Equal("Концерт Джаз", events[0].Title);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task DeleteEvent_CascadeDeletesOnlyItsOwnBookings()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var event1 = new Event("Событие 1", "Описание 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        var event2 = new Event("Событие 2", "Описание 2", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 20);

        context.Events.AddRange(event1, event2);
        await context.SaveChangesAsync();

        var event1Id = event1.Id;
        var event2Id = event2.Id;

        var bookingRepository = new BookingRepository(context);
        
        await bookingRepository.CreateBookingAsync(event1Id);
        await bookingRepository.CreateBookingAsync(event1Id);

        await bookingRepository.CreateBookingAsync(event2Id);
        await bookingRepository.CreateBookingAsync(event2Id);

        
        await using var verifyContext = CreateContext();
        var verifyEventRepository = new EventRepository(verifyContext);

        var totalBookings = await verifyContext.Bookings.CountAsync();
        Assert.Equal(4, totalBookings);
        
        await using var deleteContext = CreateContext();

        var eventToDelete = await deleteContext.Events.FirstAsync(e => e.Id == event1Id);
        
        await verifyEventRepository.DeleteEventByIdAsync(eventToDelete.Id);
        
        await using var finalContext = CreateContext();

        var remainingBookings = await finalContext.Bookings.ToListAsync();
        Assert.Equal(2, remainingBookings.Count);

        var event2StillExists = await finalContext.Events.AnyAsync(e => e.Id == event2Id);
        Assert.True(event2StillExists);

        var event1StillExists = await finalContext.Events.AnyAsync(e => e.Id == event1Id);
        Assert.False(event1StillExists);
    }
}