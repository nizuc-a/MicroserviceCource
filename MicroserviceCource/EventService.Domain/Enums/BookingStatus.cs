using System.ComponentModel;

namespace EventService.Domain.Enums;

public enum BookingStatus
{
    [Description("Бронь создана, ожидает обработки")]
    Pending,
    [Description("Бронь подтверждена")]
    Confirmed,
    [Description("Бронь отклонена")]
    Rejected
}