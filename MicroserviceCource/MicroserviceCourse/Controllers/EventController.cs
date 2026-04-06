using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviceCourse.Controllers;

[ApiController]
[Route("events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult>> GetAll(
        [FromQuery] string? title = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var events = await eventService.GetAll(title, from, to, page, pageSize);
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Event>> GetEventById(Guid id)
    {
        var value = await eventService.GetById(id);

        return Ok(value);
    }

    [HttpPost]
    public async Task<ActionResult<Event>> AddEvent([FromBody] AddEventDto dto)
    {
        var result = await eventService.AddEvent(dto);

        return CreatedAtAction(nameof(GetEventById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventDto dto)
    {
        await eventService.UpdateEvent(id, dto);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        await eventService.DeleteEventById(id);
        
        return NoContent();
    }
}