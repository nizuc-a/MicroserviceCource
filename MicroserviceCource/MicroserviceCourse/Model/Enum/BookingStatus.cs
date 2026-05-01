using System.ComponentModel;

namespace MicroserviceCourse.Model.Enum;

public enum BookingStatus
{
    [Description("Бронь создана, ожидает обработки")]
    Pending,
    [Description("Бронь подтверждена")]
    Confirmed,
    [Description("Бронь отклонена")]
    Rejected
}