using MicroserviceCourse.Data;
using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventService.Tests;

public class EventServiceTests
{
    private AppDbContext _dbContext;
    private List<Event> _events;
    private MicroserviceCourse.Services.EventService _eventService;

    private static (string, string)[] dates =
    [
        ("22.03.2010", "27.06.2011"),
        ("22.02.2011", "22.12.2014"),
        ("06.03.2015", "22.07.2021")
    ];
    
    public EventServiceTests()
    {
        _events = new()
        {
            new Event("крещение Руси", "988 год", DateTime.Parse(dates[0].Item1), DateTime.Parse(dates[0].Item2))
            {
                Id = 1,
            },
            new Event("битва на реке Калке", "1223 год", DateTime.Parse(dates[1].Item1), DateTime.Parse(dates[1].Item2))
            {
                Id = 2,
            },
            new Event("Отечественная война", "1812 год", DateTime.Parse(dates[2].Item1), DateTime.Parse(dates[2].Item2))
            {
                Id = 3,
            },
        };
        
        SetupDbContext();
        
        _eventService = new MicroserviceCourse.Services.EventService(_dbContext);
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

    #region GetEvents
    
    [Fact]
    public async Task GetAll_Correct()
    {
        var expectedResult = new PaginatedResult()
        {
            PageNumber = 1,
            AllElementCount = _events.Count,
            CurrentPageElementCount = 3,
            Events = _events.ToArray()
        };
        
        var result = await _eventService.GetAll();
        
        Assert.Equal(expectedResult.PageNumber, result.PageNumber);
        Assert.Equal(expectedResult.AllElementCount, result.AllElementCount);
        Assert.Equal(expectedResult.CurrentPageElementCount, result.CurrentPageElementCount);
        Assert.Equal(expectedResult.Events.Length, result.Events.Length);
    }

    [Fact]
    public async Task GetEvents_TitleFilter()
    {
        var title = "война";

        var expectedResult = new PaginatedResult()
        {
            PageNumber = 1,
            AllElementCount = _events.Count,
            CurrentPageElementCount = 1,
            Events = [_events[2]]
        };
        
        var result = await _eventService.GetAll(title);
        
        Assert.Equal(expectedResult.PageNumber, result.PageNumber);
        Assert.Equal(expectedResult.AllElementCount, result.AllElementCount);
        Assert.Equal(expectedResult.CurrentPageElementCount, result.CurrentPageElementCount);
        Assert.Equal(expectedResult.Events.Length, result.Events.Length);
    }
    
    [Theory]
    [InlineData("22.04.2010", null, 2)]
    [InlineData(null, "22.12.2015", 2)]
    [InlineData("22.04.2010", "22.12.2015", 1)]
    public async Task GetEvents_DateFilter(string? from, string? to, int expectedCount)
    {
        var fromDate = from  != null ? DateTime.Parse(from) : (DateTime?)null;
        var toDate =  to != null ? DateTime.Parse(to) : (DateTime?)null;
        
        var result = await _eventService.GetAll( from: fromDate,  to: toDate);
        
        Assert.Equal(expectedCount, result.Events.Length);
    }
    
    [Fact]
    public async Task GetEvents_CombinedFilter()
    {
        var title = "война";
        var dateFrom = DateTime.Parse("22.04.2010");
        
        var expectedResult = new PaginatedResult()
        {
            PageNumber = 1,
            AllElementCount = _events.Count,
            CurrentPageElementCount = 1,
            Events = [_events[2]]
        };
        
        var result = await _eventService.GetAll(title, dateFrom);
        
        Assert.Equal(expectedResult.PageNumber, result.PageNumber);
        Assert.Equal(expectedResult.AllElementCount, result.AllElementCount);
        Assert.Equal(expectedResult.CurrentPageElementCount, result.CurrentPageElementCount);
        Assert.Equal(expectedResult.Events.Length, result.Events.Length);
    }
    
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public async Task GetEvents_Pagination_ArgumentOutOfRangeException(int pageNumber, int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _eventService.GetAll(pageNumber: pageNumber, pageSize: pageSize));
    }
    
    #endregion

    #region GetById

    [Fact]
    public async Task GetById_Correct()
    {
        var id = 1;
        
        var result = await _eventService.GetById(id);
        
        Assert.NotNull(result);
    }
    
    [Fact]
    public async Task GetById_ArgumentNullException()
    {
        var id = 0;
        
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _eventService.GetById(id));
    }

    #endregion

    #region AddEvent

    [Fact]
    public async Task AddEvent_Correct()
    {
        var addEventDto = new AddEventDto()
        {
            Title = "Title",
            Description = "Description",
            StartAt = DateTime.Now.AddMonths(-1),
            EndAt = DateTime.Now.AddMonths(1),
        };
        
        var result = await _eventService.AddEvent(addEventDto);
        
        Assert.Equal(addEventDto.Title, result.Title);
        Assert.Equal(addEventDto.Description, result.Description);
        Assert.Equal(addEventDto.StartAt, result.StartAt);
        Assert.Equal(addEventDto.EndAt, result.EndAt);
    }
    
    [Fact]
    public async Task AddEvent_ArgumentOutOfRangeException()
    {
        var addEventDto = new AddEventDto()
        {
            Title = "Title",
            Description = "Description",
            StartAt = DateTime.Now.AddMonths(1),
            EndAt = DateTime.Now.AddMonths(-1),
        };
        
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _eventService.AddEvent(addEventDto));
    }

    #endregion

    #region UpdateEvent

    [Fact]
    public async Task UpdateEvent_Correct()
    {
        int id = 1;
        var dto = new UpdateEventDto()
        {
            Title = "Title",
            Description = "Description",
            StartAt = DateTime.Now.AddMonths(-1),
            EndAt = DateTime.Now.AddMonths(1),
        };
        
        await _eventService.UpdateEvent(id, dto);
        
        var entity = await _eventService.GetById(id);
        
        Assert.NotNull(entity);
        Assert.Equal(dto.Title, entity.Title);
        Assert.Equal(dto.Description, entity.Description);
        Assert.Equal(dto.StartAt, entity.StartAt);
        Assert.Equal(dto.EndAt, entity.EndAt);
    }
    
    [Fact]
    public async Task UpdateEvent_ArgumentOutOfRangeException()
    {
        int id = 1;
        var dto = new UpdateEventDto()
        {
            Title = "Title",
            Description = "Description",
            StartAt = DateTime.Now.AddMonths(1),
            EndAt = DateTime.Now.AddMonths(-1),
        };
        
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _eventService.UpdateEvent(id,dto));
    }
    
    [Fact]
    public async Task UpdateEvent_ArgumentNullException()
    {
        int id = -1;
        var dto = new UpdateEventDto()
        {
            Title = "Title",
            Description = "Description",
            StartAt = DateTime.Now.AddMonths(-1),
            EndAt = DateTime.Now.AddMonths(1),
        };
        
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _eventService.UpdateEvent(id,dto));
    }

    #endregion

    #region DeleteEvent

    [Fact]
    public async Task DeleteEventById_Correct()
    {
        var id = 1;
        
        await _eventService.DeleteEventById(id);
        var eventsAfterDelete = await _eventService.GetAll();
        
        Assert.Equal(2,  eventsAfterDelete.AllElementCount);
    }
    
    [Fact]
    public async Task DeleteEventById_ArgumentNullException()
    {
        var id = -1;
        
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _eventService.DeleteEventById(id));
    }

    #endregion
}