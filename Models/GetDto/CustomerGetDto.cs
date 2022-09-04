using System.ComponentModel.DataAnnotations;

namespace Models.GetDto;

public class CustomerGetDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string FullName => FirstName + " " + LastName;

    [Required]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
