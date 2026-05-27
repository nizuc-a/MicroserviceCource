using EventService.Application.Abstractions.Services;
using EventService.Application.Abstractions.TaskQueue;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Api.Controllers;

[ApiController]
[Route("bookings")]
public class BookingController(IBookingService bookingService, IBookingTaskQueue bookingTaskQueue) : ControllerBase
{
    [HttpPost("/events/{eventId:guid}/book")]
    public async Task<IActionResult> AddBooking([FromRoute] Guid eventId, CancellationToken ct)
    {
        Booking newBooking;
        try
        {
            newBooking = await bookingService.CreateBookingAsync(eventId, ct);
        }
        catch (NoAvailableSeatsException)
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