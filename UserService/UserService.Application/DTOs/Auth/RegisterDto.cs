using System.ComponentModel.DataAnnotations;
using Shared.Domain.Enums;

namespace UserService.Application.DTOs.Auth;

public class RegisterDto
{
    [Required] 
    [MinLength(3)] 
    public string Login { get; set; } = "";
    
    [Required]
    [MinLength(3)]
    public string Password { get; set; } = "";
    
    public UserRole Role { get; set; } =  UserRole.User;
}