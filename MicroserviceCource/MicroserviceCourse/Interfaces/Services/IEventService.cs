using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;

namespace MicroserviceCourse.Interfaces.Services;

public interface IEventService
{
    /// <summary>
    /// Получить все события.
    /// </summary>
    /// <returns>Список событий.</returns>
    Task<PaginatedResult> GetAll(string? title = null, DateTime? from= null, DateTime? to= null, int page = 1, int pageSize = 10);

    /// <summary>
    /// Получить событие по идентификатору.
    /// </summary>
    /// <param name="id">идентификатор</param>
    /// <returns>Событие.</returns>
    Task<Event> GetById(int id);

    /// <summary>
    /// Добавить событие.
    /// </summary>
    /// <param name="data">Событие которое нужно добавить</param>
    /// <returns>True - если операция прошла успешно, False - если нет.</returns>
    Task<Event> AddEvent(AddEventDto data);

    /// <summary>
    /// Обновить событие.
    /// </summary>
    /// <param name="id">id обновляемой сущности</param>
    /// <param name="data">Источник обновления</param>
    /// <returns>True - если операция прошла успешно, False - если нет.</returns>
    Task UpdateEvent(int id, UpdateEventDto data);
    
    /// <summary>
    /// Удалить событие.
    /// </summary>
    /// <param name="id">Id события которое нужно удалить</param>
    /// <returns>True - если операция прошла успешно, False - если нет.</returns>
    Task DeleteEventById(int id);
}