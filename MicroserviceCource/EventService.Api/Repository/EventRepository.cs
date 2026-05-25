using EventService.Api.Data;
using EventService.Api.Interfaces.Repository;
using EventService.Api.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventService.Api.Repository;

public class EventRepository(AppDbContext context) : IEventRepository
{
    public async Task<(Event[], int)> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10,
        CancellationToken ct = default)
    {
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

        return new (events, allElementCount);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task AddEventAsync(Event data, CancellationToken ct = default)
    {
        await context.Events.AddAsync(data, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateEvent(Event data, CancellationToken ct = default)
    {
        context.Events.Update(data);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteEventByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if(entity == null)
            return;
        
        context.Events.Remove(entity);
        await context.SaveChangesAsync(ct);
    }
}