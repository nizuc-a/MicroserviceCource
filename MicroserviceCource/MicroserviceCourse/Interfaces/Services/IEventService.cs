using MicroserviceCourse.Model.Entity;

namespace MicroserviceCourse.Interfaces.Services;

public interface IEventService
{
    /// <summary>
    /// Получить все события.
    /// </summary>
    /// <returns>Список событий.</returns>
    Task<IEnumerable<Event>> GetAll();

    /// <summary>
    /// Получить событие по идентификатору.
    /// </summary>
    /// <param name="id">идентификатор</param>
    /// <returns>Событие.</returns>
    Task<Event?> GetById(int id);

    /// <summary>
    /// Добавить событие.
    /// </summary>
    /// <param name="data">Событие которое нужно добавить</param>
    /// <returns>True - если операция прошла успешно, False - если нет.</returns>
    Task<Event> AddEvent(Event data);

    /// <summary>
    /// Обновить событие.
    /// </summary>
    /// <param name="data">Событие которое нужно обновить</param>
    /// <returns>True - если операция прошла успешно, False - если нет.</returns>
    Task UpdateEvent(Event data);
    
    /// <summary>
    /// Удалить событие.
    /// </summary>
    /// <param name="data">Событие которое нужно удалить</param>
    /// <returns>True - если операция прошла успешно, False - если нет.</returns>
    Task DeleteEvent(Event data);
}