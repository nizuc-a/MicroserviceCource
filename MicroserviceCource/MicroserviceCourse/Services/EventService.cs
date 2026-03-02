using MicroserviceCourse.Data;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceCourse.Services;

public class EventService(AppDbContext context) : IEventService
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Event>> GetAll()
    {
        return await _context.Events.ToListAsync();
    }

    public async Task<Event?> GetById(int id)
    {
        return await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event> AddEvent(AddEventDto dto)
    {
        Event data = new Event(dto.Title, dto.Description ?? "", dto.StartAt, dto.EndAt);
        _context.Events.Add(data);
        await _context.SaveChangesAsync();
        return data;
    }

    public async Task<IActionResult> UpdateEvent(int id, UpdateEventDto data)
    {
        var entity = await GetById(id);
        if(entity == null)
            return new NotFoundResult();
        
        entity.Update(data.Title, data.Description, data.StartAt, data.EndAt);
        
        _context.Events.Update(entity);
        await _context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> DeleteEventById(int id)
    {
        var entity = await GetById(id);
        if(entity == null)
            return new NotFoundResult();
        
        _context.Events.Remove(entity);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}