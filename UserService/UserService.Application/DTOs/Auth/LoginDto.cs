using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs.Auth;

public class LoginDto
{
    [Required] 
    [MinLength(3)] 
    public string Login { get; set; } = "";
    
    [Required]
    [MinLength(3)]
    public string Password { get; set; } = "";
}