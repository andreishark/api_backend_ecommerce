using System.ComponentModel.DataAnnotations;

namespace Models.GetDto;

public record CatalogItemGetDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid Id { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string ImageLocation { get; set; } = string.Empty;
}