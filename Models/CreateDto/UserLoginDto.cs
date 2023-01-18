using System.ComponentModel.DataAnnotations;

namespace Models.CreateDto;

public record UserLoginDto
{
    [Required] public string Username { get; set; } = null!;

    [Required] public string Password { get; set; } = null!;
};