using System.Security.Cryptography;
using System.Text;
using UserService.Application.Abstractions.Auth;

namespace UserService.Infrastructure.Auth;

public class Sha256PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}