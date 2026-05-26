using EventService.Api.Interfaces.Services;
using EventService.Api.Model.DTO.Event;
using EventService.Api.Model.DTO.Pagination;
using EventService.Api.Model.Entity;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<Event>>> GetAll(
        [FromQuery] string? title = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var events = await eventService.GetAll(title, from, to, page, pageSize, ct);
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Event>> GetEventById(Guid id, CancellationToken ct = default)
    {
        var value = await eventService.GetById(id, ct);

        return Ok(value);
    }

    [HttpPost]
    public async Task<ActionResult<Event>> AddEvent([FromBody] AddEventDto dto, CancellationToken ct = default)
    {
        var result = await eventService.AddEvent(dto, ct);

        return CreatedAtAction(nameof(GetEventById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventDto dto, CancellationToken ct = default)
    {
        await eventService.UpdateEvent(id, dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken ct = default)
    {
        await eventService.DeleteEventById(id,ct);
        
        return NoContent();
    }
}