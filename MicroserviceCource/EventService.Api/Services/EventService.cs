using EventService.Api.Interfaces.Repository;
using EventService.Api.Interfaces.Services;
using EventService.Api.Model.DTO.Event;
using EventService.Api.Model.DTO.Pagination;
using EventService.Api.Model.Entity;

namespace EventService.Api.Services;

public class EventService(IEventRepository eventRepository) : IEventService
{
    public async Task<PaginatedResult<Event>> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        
        var result =  await eventRepository.GetAll(title, from, to, page, pageSize, ct);
        
        return new PaginatedResult<Event>
        {
            Items = result.Item1,
            TotalCount =  result.Item2,
            Page = page,
            PageSize =  pageSize,
        };
    }

    public async Task<Event> GetById(Guid id, CancellationToken ct = default)
    {
        var entity = await eventRepository.GetByIdAsync(id, ct);
        
        return entity ?? throw new KeyNotFoundException($"Event with Id {id} not found");
    }

    public async Task<Event> AddEvent(AddEventDto dto, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dto.StartAt, dto.EndAt);
        
        ArgumentOutOfRangeException.ThrowIfLessThan(dto.TotalSeats, 1);
        
        Event data = new Event(dto.Title, dto.Description ?? "", dto.StartAt, dto.EndAt, dto.TotalSeats);
        
        await eventRepository.AddEventAsync(data, ct);
        
        return data;
    }

    public async Task UpdateEvent(Guid id, UpdateEventDto data, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.StartAt, data.EndAt);
        
        ArgumentOutOfRangeException.ThrowIfLessThan(data.TotalSeats, 1);
        
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.AvailableSeats, data.TotalSeats);
        ArgumentOutOfRangeException.ThrowIfLessThan(data.AvailableSeats, 0);
        
        
        var entity = await GetById(id, ct);
        
        entity.Update(data.Title, data.Description ?? "", data.StartAt, data.EndAt, data.TotalSeats, data.AvailableSeats);
        
        await eventRepository.UpdateEvent(entity, ct);
    }

    public async Task DeleteEventById(Guid id, CancellationToken ct = default)
    {
        await eventRepository.DeleteEventByIdAsync(id, ct);
    }
}