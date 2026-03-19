using MicroserviceCourse.Data;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceCourse.Services;

public class EventService(AppDbContext context) : IEventService
{
    public async Task<PaginatedResult> GetAll(string? title, DateTime? from, DateTime? to, int pageNumber = 1, int pageSize = 10)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var result = new PaginatedResult();
        result.PageNumber = pageNumber;
        
        var query = context.Events.AsQueryable();
        
        result.AllElementCount =  query.Count();
        
        if(!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));
        
        if(from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);
        
        if(to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);
        
        query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        
        result.Events = await query.ToArrayAsync();
        result.CurrentPageElementCount = result.Events.Length;
        
        return result;
    }

    public async Task<Event?> GetById(int id)
    {
        return await context.Events.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event> AddEvent(AddEventDto dto)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dto.StartAt, dto.EndAt);
        
        Event data = new Event(dto.Title, dto.Description ?? "", dto.StartAt, dto.EndAt);
        context.Events.Add(data);
        await context.SaveChangesAsync();
        return data;
    }

    public async Task<IActionResult> UpdateEvent(int id, UpdateEventDto data)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.StartAt, data.EndAt);
        
        var entity = await GetById(id);
        if(entity == null)
            return new NotFoundResult();
        
        entity.Update(data.Title, data.Description, data.StartAt, data.EndAt);
        
        context.Events.Update(entity);
        await context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> DeleteEventById(int id)
    {
        var entity = await GetById(id);
        if(entity == null)
            return new NotFoundResult();
        
        context.Events.Remove(entity);
        await context.SaveChangesAsync();
        return new OkResult();
    }
}