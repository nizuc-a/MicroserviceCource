using EventService.Domain.Entities;

namespace EventService.Application.Abstractions.Auth;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}