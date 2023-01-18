using System.ComponentModel.DataAnnotations;

namespace Models.CreateDto;

public record UserCreateDto
{
    [Required] public string Username { get; init; } = null!;

    [Required] public string Password { get; init; } = null!;

    [Required] public string EmailAddress { get; init; } = null!;

    [Required] public string Role { get; init; } = null!;
};