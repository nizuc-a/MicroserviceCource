using System.Security.Cryptography;
using System.Text;
using EventService.Application.Abstractions.Auth;

namespace EventService.Infrastructure.Auth;

public class Sha256PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}