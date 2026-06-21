using EventService.Application.Abstractions.Services;
using EventService.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        await authService.RegisterAsync(dto.Login, dto.Password, dto.Role, ct);

        return NoContent();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var token = await authService.LoginAsync(dto.Login, dto.Password, ct);

        return Ok(token);
    }
}
