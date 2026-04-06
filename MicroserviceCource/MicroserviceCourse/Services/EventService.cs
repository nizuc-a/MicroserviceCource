using MicroserviceCourse.Data;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceCourse.Services;

public class EventService(AppDbContext context) : IEventService
{
    public async Task<PaginatedResult> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10)
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
        
        result.AllElementCount =  query.Count();
        
        query = query.Skip((page - 1) * pageSize).Take(pageSize);
        
        result.Events = await query.ToArrayAsync();
        result.CurrentPageElementCount = result.Events.Length;
        
        return result;
    }

    public async Task<Event> GetById(Guid id)
    {
        var entity = await context.Events.FirstOrDefaultAsync(e => e.Id == id);
        
        return entity ?? throw new KeyNotFoundException($"Event with Id {id} not found");
    }

    public async Task<Event> AddEvent(AddEventDto dto)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dto.StartAt, dto.EndAt);
        
        Event data = new Event(dto.Title, dto.Description ?? "", dto.StartAt, dto.EndAt);
        context.Events.Add(data);
        await context.SaveChangesAsync();
        return data;
    }

    public async Task UpdateEvent(Guid id, UpdateEventDto data)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.StartAt, data.EndAt);
        
        var entity = await GetById(id);
        
        entity.Update(data.Title, data.Description, data.StartAt, data.EndAt);
        
        context.Events.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task DeleteEventById(Guid id)
    {
        var entity = await GetById(id);
        
        context.Events.Remove(entity);
        await context.SaveChangesAsync();
    }
}