namespace BookingService.Domain.Options;

public class UserSettings
{
    public const string SectionName = "User";
    public int MaxActiveBookingsPerUser { get; set; } = 10;
}