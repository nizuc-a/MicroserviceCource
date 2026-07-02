using UserService.Domain.Entities;

namespace UserService.Application.Abstractions.Auth;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}