using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;

namespace MicroserviceCourse.Interfaces.Services;

public interface IEventService
{
    /// <summary>
    /// Получить все события.
    /// </summary>
    /// <returns>Список событий.</returns>
    Task<PaginatedResult> GetAll(string? title = null, DateTime? from= null, DateTime? to= null, int page = 1, int pageSize = 10, CancellationToken ct = default);

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
    /// Обновить событие.
    /// </summary>
    /// <param name="id">id обновляемой сущности</param>
    /// <param name="data">Источник обновления</param>
    Task UpdateEvent(Guid id, UpdateEventDto data, CancellationToken ct = default);
    
    /// <summary>
    /// Удалить событие.
    /// </summary>
    /// <param name="id">Id события которое нужно удалить</param>
    Task DeleteEventById(Guid id, CancellationToken ct = default);
}