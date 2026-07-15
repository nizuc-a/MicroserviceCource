using EventService.Application.DTOs.Event;
using EventService.Application.DTOs.Pagination;
using EventService.Domain.Entities;

namespace EventService.Application.Abstractions.Services;

public interface IEventService
{
    /// <summary>
    /// Получить все события.
    /// </summary>
    /// <returns>Список событий.</returns>
    Task<PaginatedResult<Event>> GetAll(string? title = null, DateTime? from= null, DateTime? to= null, int page = 1, int pageSize = 10, CancellationToken ct = default);

    /// <summary>
    /// Получить событие по идентификатору.
    /// </summary>
    /// <param name="id">идентификатор</param>
    /// <returns>Событие.</returns>
    Task<Event> GetById(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Добавить событие.
    /// </summary>
    /// <param name="data">Событие которое нужно добавить</param>
    Task<Event> AddEvent(AddEventDto data, CancellationToken ct = default);

    /// <summary>
    /// Получить топ событий
    /// </summary>
    /// <param name="count">Количество событий</param>
    /// <returns></returns>
    Task<Event[]> GetTop(int count, CancellationToken ct = default);

    /// <summary>
    /// Обновить событие.
    /// </summary>
    /// <param name="id">id обновляемой сущности</param>
    /// <param name="data">Источник обновления</param>
    Task UpdateEvent(Guid id, UpdateEventDto data, CancellationToken ct = default);
    
    /// <summary>
    /// Удалить событие.
    /// </summary>
    /// <param name="eventId">Id события которое нужно удалить</param>
    Task DeleteEventById(Guid eventId, CancellationToken ct = default);
    
    Task BookEvent(Guid eventId, Guid bookingId, Guid userId, int seatCount = 1, CancellationToken ct = default);

    Task ReleaseBookingAsync(Guid eventId, Guid bookingId, int seatCount, CancellationToken ct = default);

    public Task SaveChangesAsync(CancellationToken ct = default);
}