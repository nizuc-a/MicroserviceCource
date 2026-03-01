using MicroserviceCourse.Model;
using MicroserviceCourse.Model.DTO.Event;
using MicroserviceCourse.Model.Entity;

namespace MicroserviceCourse.Extension;

public static class EventExtension
{
    public static void UpdateEvent(this Event data, UpdateEventDto dto)
    {
        data.Title = dto.Title;
        data.Description = dto.Description;
        data.StartAt = dto.StartAt;
        data.EndAt = dto.EndAt;
    }

    public static Event AddEventDtoToEvent(this AddEventDto dto)
    {
        return new Event()
        {
            Title = dto.Title,
            Description = dto.Description ?? "",
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
        };
    }
}