using EventService.Api.Exceptions;
using EventService.Api.Interfaces.Services;
using EventService.Api.Interfaces.TaskQueue;
using EventService.Api.Model.Entity;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Api.Controllers;

[ApiController]
[Route("bookings")]
public class BookingController(IBookingService bookingService, IBookingTaskQueue bookingTaskQueue) : ControllerBase
{
    [HttpPost("/events/{eventId:guid}/book")]
    public async Task<IActionResult> AddBooking([FromRoute] Guid eventId, CancellationToken ct)
    {
        Booking newBooking = null;
        try
        {
            newBooking = await bookingService.CreateBookingAsync(eventId, ct);
        }
        catch (NoAvailableSeatsException e)
        {
            return Conflict($"/events/{eventId}/book");
        }

        bookingTaskQueue.Enqueue(newBooking);

        return Accepted($"/bookings/{newBooking.Id}", new
        {
            bookingId = newBooking.Id,
            eventId = newBooking.EventId,
            status = newBooking.Status
        });
    }

    [HttpGet("{bookingId:guid}")]
    public async Task<IActionResult> GetBooking([FromRoute] Guid bookingId, CancellationToken ct)
    {
        var booking = await bookingService.GetBookingByIdAsync(bookingId, ct);
        return Ok(booking);
    }
}