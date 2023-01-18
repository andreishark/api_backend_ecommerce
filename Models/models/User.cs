using System.ComponentModel.DataAnnotations;

namespace Models.models;

public record User
{
    [Required] public Guid Id { get; init; } = Guid.NewGuid();

    [Required] public string Username { get; init; } = null!;

    [Required] public string Password { get; init; } = null!;

    [Required] public string EmailAddress { get; init; } = null!;

    [Required] public string Role { get; init; } = null!;
}