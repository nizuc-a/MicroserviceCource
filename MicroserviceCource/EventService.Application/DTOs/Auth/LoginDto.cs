using System.ComponentModel.DataAnnotations;

namespace EventService.Application.DTOs.Auth;

public class LoginDto
{
    [Required]
    [MinLength(3)]
    string Login { get; set; }
    
    [Required]
    [MinLength(3)]
    string Password { get; set; }
}