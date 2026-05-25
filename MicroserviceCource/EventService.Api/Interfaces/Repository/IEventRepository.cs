using EventService.Api.Model.Entity;

namespace EventService.Api.Interfaces.Repository;

public interface IEventRepository
{
    Task<(Event[], int)> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1,
        int pageSize = 10, CancellationToken ct = default);

    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddEventAsync(Event data, CancellationToken ct = default);

    Task UpdateEvent(Event data, CancellationToken ct = default);

    Task DeleteEventByIdAsync(Guid id, CancellationToken ct = default);
}