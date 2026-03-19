using MicroserviceCourse.Data;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceCourse.Services;

public class EventService(AppDbContext context) : IEventService
{
    public async Task<IEnumerable<Event>> GetAll(string? title, DateTime? from, DateTime? to)
    {
        var query = context.Events.AsQueryable();
        
        if(!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));
        
        if(from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);
        
        if(to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);
        
        return await query.ToListAsync();
    }

    public async Task<Event?> GetById(int id)
    {
        return await context.Events.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event> AddEvent(AddEventDto dto)
    {
        Event data = new Event(dto.Title, dto.Description ?? "", dto.StartAt, dto.EndAt);
        context.Events.Add(data);
        await context.SaveChangesAsync();
        return data;
    }

    public async Task<IActionResult> UpdateEvent(int id, UpdateEventDto data)
    {
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