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
    public async Task<ActionResult<IEnumerable<Event>>> GetAll(
        [FromQuery] string? title = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var events = await eventService.GetAll(title, from, to, pageNumber, pageSize);
        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Event>> GetEventById(int id)
    {
        var value = await eventService.GetById(id);
        if (value == null)
            return NotFound();

        return Ok(value);
    }

    [HttpPost]
    public async Task<ActionResult<Event>> AddEvent([FromBody] AddEventDto dto)
    {
        var result = await eventService.AddEvent(dto);

        return CreatedAtAction(nameof(GetEventById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
    {
        await eventService.UpdateEvent(id, dto);

        return Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        await eventService.DeleteEventById(id);
        
        return Ok();
    }
}