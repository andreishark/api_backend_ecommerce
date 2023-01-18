using System.ComponentModel.DataAnnotations;

namespace Models.GetDto;

public record UserGetDto
{
    [Required] public string Username { get; init; } = null!;

    [Required] public string EmailAddress { get; init; } = null!;

    [Required] public string Role { get; init; } = null!;
};