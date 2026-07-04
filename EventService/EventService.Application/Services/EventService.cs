using System.Collections.Concurrent;
using System.Text.Json;
using EventService.Application.Abstractions.Repositories;
using EventService.Application.Abstractions.Services;
using EventService.Application.DTOs.Event;
using EventService.Application.DTOs.Pagination;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;
using Shared.Domain.Contracts.Booking;
using Shared.Domain.Contracts.Event;
using Shared.Domain.Entities;

namespace EventService.Application.Services;

public class EventService(IEventRepository eventRepository) : IEventService
{
    private const string EventsTopic = "events";
    private const string BookingsTopic = "bookings";
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new();

    public async Task<PaginatedResult<Event>> GetAll(string? title = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var result = await eventRepository.GetAll(title, from, to, page, pageSize, ct);

        return new PaginatedResult<Event>
        {
            Items = result.Item1,
            TotalCount = result.Item2,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<Event> GetById(Guid id, CancellationToken ct = default)
    {
        var entity = await eventRepository.GetByIdAsync(id, ct);

        return entity ?? throw new KeyNotFoundException($"Event with Id {id} not found");
    }

    public async Task<Event> AddEvent(AddEventDto dto, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dto.StartAt, dto.EndAt);

        ArgumentOutOfRangeException.ThrowIfLessThan(dto.TotalSeats, 1);

        Event data = new Event(dto.Title, dto.Description ?? "", dto.StartAt, dto.EndAt, dto.TotalSeats);

        await eventRepository.AddEventAsync(data, ct);

        return data;
    }

    public async Task UpdateEvent(Guid id, UpdateEventDto data, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.StartAt, data.EndAt);

        ArgumentOutOfRangeException.ThrowIfLessThan(data.TotalSeats, 1);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.AvailableSeats, data.TotalSeats);
        ArgumentOutOfRangeException.ThrowIfLessThan(data.AvailableSeats, 0);


        var entity = await GetById(id, ct);

        entity.Update(data.Title, data.Description ?? "", data.StartAt, data.EndAt, data.TotalSeats,
            data.AvailableSeats);

        await eventRepository.UpdateEvent(entity, ct);
    }

    public async Task DeleteEventById(Guid eventId, CancellationToken ct = default)
    {
        var payloadRaw = new EventDeleted(eventId);

        var outboxMessage = new OutboxMessage
        {
            Topic = EventsTopic,
            Key = eventId.ToString(),
            Type = nameof(EventDeleted),
            Payload = JsonSerializer.Serialize(payloadRaw)
        };

        await eventRepository.DeleteEventByIdAsync(eventId, outboxMessage, ct);
    }

    public async Task BookEvent(Guid eventId, Guid bookingId, Guid userId, CancellationToken ct = default)
    {
        var semaphore = _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            var result = await eventRepository.GetByIdAsync(eventId, ct);

            if (result == null)
            {
                await PublishBookingRejectedAsync(eventId, bookingId, userId, ct);
                return;
            }

            if (result.StartAt <= DateTime.UtcNow)
                throw new EventExpiredException("Event has already started");

            if (!result.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
            
            result.AddBooking(bookingId);

            await PublishBookingConfirmedAsync(eventId, bookingId, userId, ct);
        }
        catch (Exception ex) when (ex is EventExpiredException or NoAvailableSeatsException)
        {
            await PublishBookingRejectedAsync(eventId, bookingId, userId, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task PublishBookingConfirmedAsync(Guid eventId, Guid bookingId, Guid userId,
        CancellationToken ct)
    {
        var payload = new BookingConfirmed
        {
            EventId = eventId,
            BookingId = bookingId,
            UserId = userId,
        };

        var outboxMessage = new OutboxMessage
        {
            Topic = BookingsTopic,
            Key = bookingId.ToString(),
            Type = nameof(BookingConfirmed),
            Payload = JsonSerializer.Serialize(payload),
        };

        await eventRepository.AddOutboxMessageAsync(outboxMessage, ct);
    }

    private async Task PublishBookingRejectedAsync(Guid eventId, Guid bookingId, Guid userId,
        CancellationToken ct)
    {
        var payload = new BookingRejected
        {
            EventId = eventId,
            BookingId = bookingId,
            UserId = userId,
        };

        var outboxMessage = new OutboxMessage
        {
            Topic = BookingsTopic,
            Key = bookingId.ToString(),
            Type = nameof(BookingRejected),
            Payload = JsonSerializer.Serialize(payload),
        };

        await eventRepository.AddOutboxMessageAsync(outboxMessage, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => eventRepository.SaveChangesAsync(ct);
}