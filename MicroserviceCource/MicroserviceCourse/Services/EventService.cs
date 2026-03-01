using MicroserviceCourse.Data;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.Entity;
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

    public async Task<Event> AddEvent(Event data)
    {
        _context.Events.Add(data);
        await _context.SaveChangesAsync();
        return data;
    }

    public async Task UpdateEvent(Event data)
    {
        _context.Events.Update(data);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteEvent(Event data)
    {
        _context.Events.Remove(data);
        await _context.SaveChangesAsync();
    }
}