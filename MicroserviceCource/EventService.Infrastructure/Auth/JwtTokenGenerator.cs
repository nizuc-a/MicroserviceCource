using System.Security.Claims;
using System.Text;
using EventService.Application.Abstractions.Auth;
using EventService.Domain.Entities;
using EventService.Domain.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace EventService.Infrastructure.Auth;

public class JwtTokenGenerator(IOptions<JwtSettings> options) : ITokenGenerator
{
    public string GenerateToken(User user)
    {
        var settings = options.Value;

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Nickname] = user.Login,
            ["role"] = user.Role,
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