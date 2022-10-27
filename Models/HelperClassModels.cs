using Models.CreateDto;
using Models.GetDto;
using Models.models;

namespace Models;

public static class HelperClassModels
{
    public static CustomerGetDto AsDto ( this Customer customer ) => new ( )
    {
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        Id = customer.Id,
        Email = customer.Email
    };

    public static Customer AsCustomer ( this CustomerCreateDto customerDto ) => new ( )
    {
        FirstName = customerDto.FirstName,
        LastName = customerDto.LastName,
        Email = customerDto.Email,
    };

    public static CatalogItem CopyCatalogItem ( this CatalogItem item ) => new ( )
    {
        Name = item.Name,
        Id = item.Id,
        CreatedDate = item.CreatedDate,
        UpdatedDate = item.UpdatedDate,
        Price = item.Price,
        Description = item.Description,
        ImageLocation = item.ImageLocation,
    };

    public static CatalogItemGetDto AsGetDto ( this CatalogItem item ) => new ( )
    {
        Name = item.Name,
        Id = item.Id,
        Price = item.Price,
        Description = item.Description,
        ImageLocation = item.ImageLocation,
    };

    public static CatalogItem AsCatalogItem ( this CatalogItemCreateDto item, List<string> imageLocation ) => new ( )
    {
        Name = item.Name,
        Id = Guid.NewGuid ( ),
        CreatedDate = DateTime.UtcNow,
        UpdatedDate = DateTime.UtcNow,
        Price = item.Price,
        Description = item.Description,
        ImageLocation = imageLocation,
    };

    public static CatalogItemCreateDto AsCreateDto ( this CatalogItem item ) => new ( )
    {
        Name = item.Name,
        Price = item.Price,
        Description = item.Description,
    };

    public static CatalogItem AsCatalogItem ( this CatalogItemCreateDto item, List<string> imageLocation, Guid id ) => new ( )
    {
        Name = item.Name,
        Id = id,
        CreatedDate = DateTime.UtcNow,
        UpdatedDate = DateTime.UtcNow,
        Price = item.Price,
        Description = item.Description,
        ImageLocation = imageLocation,
    };

    public static CatalogItem AsCatalogItem ( this CatalogItemCreateDto item, List<string> imageLocation, Guid id, DateTimeOffset createdTime ) => new ( )
    {
        Name = item.Name,
        Id = id,
        CreatedDate = createdTime,
        UpdatedDate = DateTime.UtcNow,
        Price = item.Price,
        Description = item.Description,
        ImageLocation = imageLocation,
    };
}