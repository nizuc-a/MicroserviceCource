using System.Security.Claims;
using EventService.Application.Abstractions.Services;
using EventService.Application.Abstractions.TaskQueue;
using EventService.Domain.Entities;
using EventService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Api.Controllers;

[ApiController]
[Route("bookings")]
[Authorize]
public class BookingController(IBookingService bookingService, IBookingTaskQueue bookingTaskQueue) : ControllerBase
{
    [HttpPost("/events/{eventId:guid}/book")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> AddBooking([FromRoute] Guid eventId, CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue("userId");
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();
        
        Booking newBooking = await bookingService.CreateBookingAsync(eventId, userId, ct);

        bookingTaskQueue.Enqueue(newBooking);

        return Accepted($"/bookings/{newBooking.Id}", new
        {
            bookingId = newBooking.Id,
            eventId = newBooking.EventId,
            status = newBooking.Status
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetBookingsByUserId(CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue("userId");
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var bookings = await bookingService.GetBookingsByUserId(userId, ct);
        return Ok(bookings);
    }

    [HttpGet("{bookingId:guid}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetBooking([FromRoute] Guid bookingId, CancellationToken ct)
    {
        var booking = await bookingService.GetBookingByIdAsync(bookingId, ct);
        return Ok(booking);
    }

    [HttpDelete("{bookingId:guid}")]
    public async Task<IActionResult> CancelBooking([FromRoute] Guid bookingId, CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue("userId");
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var isAdmin = User.IsInRole(nameof(UserRole.Admin));
        await bookingService.CancelBookingAsync(bookingId, userId, isAdmin, ct);
        return NoContent();
    }
}