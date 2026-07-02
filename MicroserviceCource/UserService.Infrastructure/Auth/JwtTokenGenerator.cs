using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UserService.Application.Abstractions.Auth;
using UserService.Domain.Entities;
using UserService.Domain.Settings;

namespace UserService.Infrastructure.Auth;

public class JwtTokenGenerator(IOptions<JwtSettings> options) : ITokenGenerator
{
    public string GenerateToken(User user)
    {
        var settings = options.Value;

        var claims = new Dictionary<string, object>
        {
            ["userId"] = user.Id.ToString(),
            ["role"] = user.Role.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor()
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Claims = claims,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes),
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = creds
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}