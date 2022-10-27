using System.ComponentModel.DataAnnotations;

namespace Models.CreateDto;

public record CatalogItemCreateDto
{
    [Required]
    public string Name { get; set; } = "Placeholder";

    [Required]
    public decimal Price { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

}