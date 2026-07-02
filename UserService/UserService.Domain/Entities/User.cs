using Shared.Domain.Enums;

namespace UserService.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public UserRole Role { get; set; }
    
    public string Login { get; set; }
    
    public string PasswordHash { get; set; }

    public List<Guid> BookingIds { get; init; } = new();

    public User(string login, string passwordHash, UserRole role = UserRole.User)
    {
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }
    
    public void AddBooking(Guid bookingId)
    {
        if (!BookingIds.Contains(bookingId))
            BookingIds.Add(bookingId);
    }

    public void RemoveBooking(Guid bookingId)
    {
        BookingIds.Remove(bookingId);
    }
}