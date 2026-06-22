using EventService.Domain.Enums;

namespace EventService.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public UserRole Role { get; set; }
    
    public string Login { get; set; }
    
    public string PasswordHash { get; set; }

    public List<Booking> Bookings { get; set; } = new();

    public User(string login, string passwordHash, UserRole role = UserRole.User)
    {
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }
}