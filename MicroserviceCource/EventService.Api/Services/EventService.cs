using EventService.Api.Data;
using EventService.Api.Interfaces.Services;
using EventService.Api.Model.DTO.Event;
using EventService.Api.Model.DTO.Pagination;
using EventService.Api.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventService.Api.Services;

public class EventService(AppDbContext context) : IEventService
{
    public async Task<PaginatedResult<Event>> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        
        var query = context.Events
            .Include(x=> x.Bookings)
            .AsQueryable();
        
        if(!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));
        
        if(from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);
        
        if(to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);
        
        var allElementCount = await query.CountAsync(ct);
        
        query = query.Skip((page - 1) * pageSize).Take(pageSize);
        
        var events = await query.ToArrayAsync(ct);
        
        return new PaginatedResult<Event>()
        {
            Items = events,
            TotalCount =  allElementCount,
            Page = page,
            PageSize =  pageSize,
        };
    }

    public async Task<Event> GetById(Guid id, CancellationToken ct = default)
    {
        var entity = await context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        
        return entity ?? throw new KeyNotFoundException($"Event with Id {id} not found");
    }

    public async Task<Event> AddEvent(AddEventDto dto, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dto.StartAt, dto.EndAt);
        
        ArgumentOutOfRangeException.ThrowIfLessThan(dto.TotalSeats, 1);
        
        Event data = new Event(dto.Title, dto.Description ?? "", dto.StartAt, dto.EndAt, dto.TotalSeats);
        await context.Events.AddAsync(data, ct);
        await context.SaveChangesAsync(ct);
        return data;
    }

    public async Task UpdateEvent(Guid id, UpdateEventDto data, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.StartAt, data.EndAt);
        
        ArgumentOutOfRangeException.ThrowIfLessThan(data.TotalSeats, 1);
        
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.AvailableSeats, data.TotalSeats);
        ArgumentOutOfRangeException.ThrowIfLessThan(data.AvailableSeats, 0);
        
        
        var entity = await GetById(id, ct);
        
        entity.Update(data.Title, data.Description, data.StartAt, data.EndAt, data.TotalSeats, data.AvailableSeats);
        
        context.Events.Update(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteEventById(Guid id, CancellationToken ct = default)
    {
        var entity = await GetById(id, ct);
        
        context.Events.Remove(entity);
        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}