using EventService.Api.Model.Entity;

namespace EventService.Api.Interfaces.Repository;

public interface IEventRepository
{
    Task<(Event[], int)> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1,
        int pageSize = 10, CancellationToken ct = default);

    Task<Event?> GetById(Guid id, CancellationToken ct = default);

    Task AddEvent(Event data, CancellationToken ct = default);

    Task UpdateEvent(Event data, CancellationToken ct = default);

    Task DeleteEventById(Guid id, CancellationToken ct = default);
}