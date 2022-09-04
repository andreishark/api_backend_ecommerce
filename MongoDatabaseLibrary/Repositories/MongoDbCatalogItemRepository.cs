using System.Text.Json;
using DatabaseLibrary;
using Microsoft.AspNetCore.JsonPatch;
using Models;
using Models.CreateDto;
using Models.models;
using MongoDatabaseLibrary.Factories;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace MongoDatabaseLibrary.Repositories;

public class MongoDbCatalogItemRepository : ICatalogItemRepository
{
    private readonly IMongoCollection<CatalogItem> _catalogItemCollection;
    private readonly IMongoCollection<CatalogItem> _archive;
    private const string databaseName = "catalog";
    private const string collectionName = "items";
    private readonly FilterDefinitionBuilder<CatalogItem> filterBuilderCatalogItem = Builders<CatalogItem>.Filter;
    private readonly UpdateDefinitionBuilder<CatalogItem> updateBuilderCatalogItem = Builders<CatalogItem>.Update;

    private readonly FindOneAndReplaceOptions<CatalogItem> replaceOptions = new ( )
    {
        ReturnDocument = ReturnDocument.After
    };
    private readonly FindOneAndUpdateOptions<CatalogItem> updateOptions = new ( )
    {
        ReturnDocument = ReturnDocument.After
    };

    public MongoDbCatalogItemRepository ( IMongoDbFactory mongoDbFactory )
    {
        _catalogItemCollection = mongoDbFactory.GetCollection<CatalogItem> ( databaseName, collectionName );
        _archive = mongoDbFactory.GetArchiveCollection<CatalogItem> ( databaseName );
    }


    public async Task<CatalogItem?> CreateCatalogItem ( CatalogItem item )
    {
        await _catalogItemCollection.InsertOneAsync ( item );
        return item;
    }

    public async Task<IEnumerable<CatalogItem>?> GetAllCatalogItems ( )
    {
        var items = await _catalogItemCollection.Find ( filter: new BsonDocument ( ) ).ToListAsync ( );

        if ( items.Count == 0 ) { return null; }

        return items;
    }

    public async Task<CatalogItem?> GetCatalogItemById ( Guid id )
    {
        var filter = filterBuilderCatalogItem.Eq ( item => item.Id, id );
        return await _catalogItemCollection.Find ( filter ).SingleOrDefaultAsync ( );
    }

    public async Task<CatalogItem?> UpdateCatalogItemNameById ( string newName, Guid id )
    {
        var update = updateBuilderCatalogItem.Set ( item => item.Name, newName ).Set ( item => item.UpdatedDate, DateTime.UtcNow );
        var filter = filterBuilderCatalogItem.Eq ( item => item.Id, id );

        return await _catalogItemCollection.FindOneAndUpdateAsync ( filter, update, updateOptions );
    }

    public async Task<CatalogItem?> UpdateCatalogItemPriceById ( decimal newPrice, Guid id )
    {
        var update = updateBuilderCatalogItem.Set ( item => item.Price, newPrice ).Set ( item => item.UpdatedDate, DateTime.UtcNow );
        var filter = filterBuilderCatalogItem.Eq ( item => item.Id, id );

        return await _catalogItemCollection.FindOneAndUpdateAsync ( filter, update, updateOptions );
    }

    public async Task<CatalogItem?> UpdateCatalogItemDescriptionById ( string newDescription, Guid id )
    {
        var update = updateBuilderCatalogItem.Set ( item => item.Description, newDescription ).Set ( item => item.UpdatedDate, DateTime.UtcNow );
        var filter = filterBuilderCatalogItem.Eq ( item => item.Id, id );

        return await _catalogItemCollection.FindOneAndUpdateAsync ( filter, update, updateOptions );
    }

    public async Task<CatalogItem?> UpdateCatalogItemImageById ( string newImageLocation, Guid id )
    {
        var update = updateBuilderCatalogItem.Set ( item => item.ImageLocation, newImageLocation ).Set ( item => item.UpdatedDate, DateTime.UtcNow );
        var filter = filterBuilderCatalogItem.Eq ( item => item.Id, id );

        return await _catalogItemCollection.FindOneAndUpdateAsync ( filter, update, updateOptions );
    }

    public async Task<CatalogItem?> DeleteCatalogItemById ( Guid id )
    {
        var filter = filterBuilderCatalogItem.Eq ( item => item.Id, id );

        var item = await _catalogItemCollection.FindOneAndDeleteAsync ( filter );

        if ( item is null ) return null;

        var copy = item.CopyCatalogItem ( );
        copy.Name = $"{copy.Name}_archived";

        await _archive.InsertOneAsync ( copy );
        return item;
    }

    public async Task<CatalogItem?> ReplaceCatalogItemById ( CatalogItem newItem, Guid id )
    {
        var filter = filterBuilderCatalogItem.Eq ( item => item.Id, id );

        var oldItem = await _catalogItemCollection.Find ( filter ).SingleOrDefaultAsync ( );

        if ( oldItem is null ) return null;

        CatalogItem curatedItem = new ( )
        {
            Name = newItem.Name,
            Id = id,
            CreatedDate = oldItem.CreatedDate,
            UpdatedDate = DateTime.UtcNow,
            Price = newItem.Price,
            Description = newItem.Description,
            ImageLocation = newItem.ImageLocation,
        };

        return await _catalogItemCollection.FindOneAndReplaceAsync ( filter, curatedItem, replaceOptions );
    }
}