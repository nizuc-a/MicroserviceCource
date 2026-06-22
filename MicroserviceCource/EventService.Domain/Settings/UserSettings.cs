namespace EventService.Domain.Settings;

public class UserSettings
{
    public const string SectionName = "User";
    public int MaxActiveBookingsPerUser { get; set; } = 10;
}