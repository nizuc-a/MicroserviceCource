using MicroserviceCourse.Extension;
using MicroserviceCourse.Interfaces.Services;
using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviceCourse.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetAll()
    {
        var events = await _eventService.GetAll();
        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Event>> GetEventById(int id)
    {
        var value = await _eventService.GetById(id);
        if (value == null)
            return NotFound();
        
        return Ok(value);
    }

    [HttpPost]
    public async Task<ActionResult<Event>> AddEvent([FromBody]AddEventDto dto)
    {
        if (dto.StartAt > dto.EndAt)
            return BadRequest("StartAt must be less than EndAt");

        var data = dto.AddEventDtoToEvent();
        await _eventService.AddEvent(data);

        return CreatedAtAction(nameof(GetEventById), new { id = data.Id }, data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Event>> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
    {
        var data = await _eventService.GetById(id);
        if (data is null)
            return NotFound();
        
        if (dto.StartAt > dto.EndAt)
            return BadRequest("StartAt must be less than EndAt");
        
        data.UpdateEvent(dto);
        await _eventService.UpdateEvent(data);
        return Ok(data);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteEvent(int id)
    {
        var data = await _eventService.GetById(id);
        if (data is null)
            return NotFound();
        
        await _eventService.DeleteEvent(data);
        return NoContent();
    }
}