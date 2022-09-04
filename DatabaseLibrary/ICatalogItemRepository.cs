using Models.models;

namespace DatabaseLibrary;

public interface ICatalogItemRepository
{
    public Task<IEnumerable<CatalogItem>?> GetAllCatalogItems ( );

    public Task<CatalogItem?> GetCatalogItemById ( Guid id );

    public Task<CatalogItem?> CreateCatalogItem ( CatalogItem item );

    public Task<CatalogItem?> UpdateCatalogItemNameById ( string newName, Guid id );

    public Task<CatalogItem?> UpdateCatalogItemPriceById ( decimal newPrice, Guid id );

    public Task<CatalogItem?> UpdateCatalogItemDescriptionById ( string newDescription, Guid id );

    public Task<CatalogItem?> UpdateCatalogItemImageById ( string newImageLocation, Guid id );

    public Task<CatalogItem?> ReplaceCatalogItemById ( CatalogItem newItem, Guid id );

    public Task<CatalogItem?> DeleteCatalogItemById ( Guid id );
}