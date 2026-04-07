using MicroserviceCourse.Data;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceCourse.Services;

public class EventService(AppDbContext context) : IEventService
{
    public async Task<PaginatedResult> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var result = new PaginatedResult();
        result.Page = page;
        
        var query = context.Events.AsQueryable();
        
        if(!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));
        
        if(from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);
        
        if(to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);
        
        result.AllElementCount = await query.CountAsync(ct);
        
        query = query.Skip((page - 1) * pageSize).Take(pageSize);
        
        result.Events = await query.ToArrayAsync(ct);
        result.CurrentPageElementCount = result.Events.Length;
        
        return result;
    }

    public async Task<Event> GetById(Guid id, CancellationToken ct = default)
    {
        var entity = await context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        
        return entity ?? throw new KeyNotFoundException($"Event with Id {id} not found");
    }

    public async Task<Event> AddEvent(AddEventDto dto, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dto.StartAt, dto.EndAt);
        
        Event data = new Event(dto.Title, dto.Description ?? "", dto.StartAt, dto.EndAt);
        await context.Events.AddAsync(data, ct);
        await context.SaveChangesAsync(ct);
        return data;
    }

    public async Task UpdateEvent(Guid id, UpdateEventDto data, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.StartAt, data.EndAt);
        
        var entity = await GetById(id, ct);
        
        entity.Update(data.Title, data.Description, data.StartAt, data.EndAt);
        
        context.Events.Update(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteEventById(Guid id, CancellationToken ct = default)
    {
        var entity = await GetById(id, ct);
        
        context.Events.Remove(entity);
        await context.SaveChangesAsync(ct);
    }
}