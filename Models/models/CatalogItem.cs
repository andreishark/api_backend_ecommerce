using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Models.models;

public record CatalogItem
{
    [Required]
    [BsonElement ( "Name" )]
    [BsonRequired]
    public string Name { get; set; } = "Placeholder";

    [BsonId]
    [BsonElement ( "Id" )]
    [Required]
    [BsonRequired]
    public Guid Id { get; init; } = Guid.NewGuid ( );

    [BsonElement ( "CreatedDate" )]
    [Required]
    [BsonRequired]
    public DateTimeOffset CreatedDate { get; init; } = DateTime.UtcNow;

    [BsonElement ( "Updated Date" )]
    [Required]
    [BsonRequired]
    public DateTimeOffset UpdatedDate { get; set; } = DateTime.UtcNow;

    [BsonElement ( "Price" )]
    [Required]
    [BsonRequired]
    public decimal Price { get; set; }

    [BsonElement ( "Description" )]
    [Required]
    [BsonRequired]
    public string Description { get; set; } = string.Empty;

    [BsonElement ( "ImageLocation" )]
    [Required]
    [BsonRequired]
    public string ImageLocation { get; set; } = string.Empty;
}