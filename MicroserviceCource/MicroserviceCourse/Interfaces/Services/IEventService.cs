using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviceCourse.Interfaces.Services;

public interface IEventService
{
    /// <summary>
    /// Получить все события.
    /// </summary>
    /// <returns>Список событий.</returns>
    Task<IEnumerable<Event>> GetAll(string? title, DateTime? from, DateTime? to);

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
    Task<Event> AddEvent(AddEventDto data);

    /// <summary>
    /// Обновить событие.
    /// </summary>
    /// <param name="id">id обновляемой сущности</param>
    /// <param name="data">Источник обновления</param>
    /// <returns>True - если операция прошла успешно, False - если нет.</returns>
    Task<IActionResult> UpdateEvent(int id, UpdateEventDto data);
    
    /// <summary>
    /// Удалить событие.
    /// </summary>
    /// <param name="id">Id события которое нужно удалить</param>
    /// <returns>True - если операция прошла успешно, False - если нет.</returns>
    Task<IActionResult> DeleteEventById(int id);
}