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
using Shared.Domain.Kafka;

namespace EventService.Application.Services;

public class EventService(IEventRepository eventRepository) : IEventService
{
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

    public async Task<Event[]> GetTop(int count, CancellationToken ct = default)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Количество событий для топа должно быть больше 0");
        
        var top = await eventRepository.GetTop(count, ct);

        return top;
    }

    public async Task<Event> AddEvent(AddEventDto dto, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dto.StartAt, dto.EndAt);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dto.StartAt, DateTime.UtcNow);
        ArgumentOutOfRangeException.ThrowIfLessThan(dto.TotalSeats, 1);

        Event data = new Event(dto.Title, dto.Description ?? "", dto.StartAt, dto.EndAt, dto.TotalSeats);

        await eventRepository.AddEventAsync(data, ct);

        return data;
    }

    public async Task UpdateEvent(Guid id, UpdateEventDto data, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.StartAt, data.EndAt);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(data.StartAt, DateTime.UtcNow);
        ArgumentOutOfRangeException.ThrowIfLessThan(data.TotalSeats, 1);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.AvailableSeats, data.TotalSeats);
        ArgumentOutOfRangeException.ThrowIfLessThan(data.AvailableSeats, 0);


        var entity = await eventRepository.GetTrackedByIdAsync(id, ct)
                     ?? throw new KeyNotFoundException($"Event with Id {id} not found");

        entity.Update(data.Title, data.Description ?? "", data.StartAt, data.EndAt, data.TotalSeats,
            data.AvailableSeats);

        await eventRepository.UpdateEvent(entity, ct);
    }

    public async Task DeleteEventById(Guid eventId, CancellationToken ct = default)
    {
        var payloadRaw = new EventDeleted(eventId);

        var outboxMessage = new OutboxMessage
        {
            Topic = KafkaTopics.Events,
            Key = eventId.ToString(),
            Type = nameof(EventDeleted),
            Payload = JsonSerializer.Serialize(payloadRaw)
        };

        await eventRepository.DeleteEventByIdAsync(eventId, outboxMessage, ct);
    }

    public async Task BookEvent(Guid eventId, Guid bookingId, Guid userId, int seatCount = 1,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(seatCount, 1);

        var semaphore = _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            var result = await eventRepository.GetTrackedByIdAsync(eventId, ct);

            if (result == null)
            {
                await PublishBookingRejectedAsync(eventId, bookingId, userId, ct);
                return;
            }

            if (result.StartAt <= DateTime.UtcNow)
                throw new EventExpiredException("Event has already started");

            if (!result.TryReserveSeats(seatCount))
                throw new NoAvailableSeatsException("No available seats for this event");

            result.AddBooking(bookingId);

            await PublishBookingConfirmedAsync(eventId, bookingId, userId, ct);
            await eventRepository.InvalidateCacheAsync(eventId);
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

    public async Task ReleaseBookingAsync(Guid eventId, Guid bookingId, int seatCount,
        CancellationToken ct = default)
    {
        var entity = await eventRepository.GetTrackedByIdAsync(eventId, ct)
                     ?? throw new KeyNotFoundException($"Event with Id {eventId} not found");

        entity.ReleaseSeats(seatCount);
        entity.RemoveBooking(bookingId);

        await eventRepository.SaveChangesAsync(ct);
        await eventRepository.InvalidateCacheAsync(eventId);
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
            Topic = KafkaTopics.Bookings,
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
            Topic = KafkaTopics.Bookings,
            Key = bookingId.ToString(),
            Type = nameof(BookingRejected),
            Payload = JsonSerializer.Serialize(payload),
        };

        await eventRepository.AddOutboxMessageAsync(outboxMessage, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => eventRepository.SaveChangesAsync(ct);
}