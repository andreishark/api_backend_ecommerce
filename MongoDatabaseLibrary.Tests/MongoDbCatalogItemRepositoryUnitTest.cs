using fakeModelsService;
using Faker;
using Models;
using Models.models;
using MongoDatabaseLibrary.Factories;
using MongoDatabaseLibrary.Repositories;
using MongoDB.Driver;
using Moq;

namespace MongoDatabaseLibrary.Tests;

public class MongoDbCatalogItemRepositoryUnitTest
{

    private static Mock<IAsyncCursor<CatalogItem>> CreateMockIAsyncCursor ( IEnumerable<CatalogItem> list )
    {
        var cursor = new Mock<IAsyncCursor<CatalogItem>> ( );

        cursor.Setup ( x => x.Current ).Returns ( list );
        cursor.SetupSequence ( x => x.MoveNext (
            It.IsAny<CancellationToken> ( ) )
        ).Returns ( true ).Returns ( false );
        cursor.SetupSequence ( x => x.MoveNextAsync (
            It.IsAny<CancellationToken> ( ) )
        ).Returns ( Task.FromResult ( true ) ).Returns ( Task.FromResult ( false ) );
        return cursor;
    }

    private Mock<IMongoCollection<CatalogItem>> CreateMockIMongoCollectionArchive ( )
    {
        var archive = new Mock<IMongoCollection<CatalogItem>> ( );

        archive.Setup ( collection => collection.InsertOneAsync (
            It.IsAny<CatalogItem> ( ),
            default,
            default
        ) );

        return archive;
    }

    private Mock<IMongoDbFactory> CreateMockIMongoDbFactory ( IMongoCollection<CatalogItem> collection )
    {
        var mongoFactory = new Mock<IMongoDbFactory> ( );

        var archive = CreateMockIMongoCollectionArchive ( );

        mongoFactory.Setup (
            factory => factory.GetCollection<CatalogItem> (
            _databaseName,
            _collectionName
        ) ).Returns ( collection );

        mongoFactory.Setup (
            factory => factory.GetArchiveCollection<CatalogItem> (
                _databaseName
    ) ).Returns ( archive.Object );

        return mongoFactory;
    }

    private static Mock<IMongoCollection<CatalogItem>> CreateMockIMongoCollectionFind ( IAsyncCursor<CatalogItem> cursor, Func<CatalogItem, bool> filter )
    {
        var collection = new Mock<IMongoCollection<CatalogItem>> ( );
        var acceptedItems = new List<CatalogItem> ( );

        cursor.ForEachAsync ( item =>
        {
            if ( filter ( item ) )
            {
                acceptedItems.Add ( item );
            }
        } );

        var filteredCursor = CreateMockIAsyncCursor ( acceptedItems );

        collection.Setup ( x => x.FindAsync (
            It.IsAny<FilterDefinition<CatalogItem>> ( ),
            It.IsAny<FindOptions<CatalogItem>> ( ),
            default
        ) ).ReturnsAsync ( filteredCursor.Object );

        return collection;
    }

    private static Mock<IMongoCollection<CatalogItem>> CreateMockIMongoCollectionUpdate ( IAsyncCursor<CatalogItem> cursor, Func<CatalogItem, bool> filter, Func<CatalogItem, CatalogItem> update )
    {
        var collection = new Mock<IMongoCollection<CatalogItem>> ( );
        var acceptedItems = new List<CatalogItem> ( );
        CatalogItem? updatedItem = null;

        cursor.ForEachAsync ( item =>
        {
            if ( filter ( item ) )
            {
                item = update ( item );
                updatedItem = item;
            }
            acceptedItems.Add ( item );
        } );

        var filteredCursor = CreateMockIAsyncCursor ( acceptedItems );

        collection.Setup ( x => x.FindAsync (
            It.IsAny<FilterDefinition<CatalogItem>> ( ),
            It.IsAny<FindOptions<CatalogItem>> ( ),
            default
        ) ).ReturnsAsync ( filteredCursor.Object );

        collection.Setup ( funcCollection => funcCollection.FindOneAndUpdateAsync (
            It.IsAny<FilterDefinition<CatalogItem>> ( ),
            It.IsAny<UpdateDefinition<CatalogItem>> ( ),
            It.Is<FindOneAndUpdateOptions<CatalogItem, CatalogItem?>> (
                option => option.ReturnDocument == ReturnDocument.After
            ),
            default
        ) ).ReturnsAsync ( updatedItem );

        return collection;
    }

    private static Mock<IMongoCollection<CatalogItem>> CreateMockIMongoCollectionInsert ( IAsyncCursor<CatalogItem> cursor, CatalogItem insertedItem )
    {
        var collection = new Mock<IMongoCollection<CatalogItem>> ( );
        var totalItems = new List<CatalogItem> ( );

        cursor.ForEachAsync ( item =>
        {
            totalItems.Add ( item );
        } );
        totalItems.Add ( insertedItem );

        var filteredCursor = CreateMockIAsyncCursor ( totalItems );

        collection.Setup ( x => x.FindAsync (
            It.IsAny<FilterDefinition<CatalogItem>> ( ),
            It.IsAny<FindOptions<CatalogItem>> ( ),
            default
        ) ).ReturnsAsync ( filteredCursor.Object );

        collection.Setup ( x => x.InsertOneAsync (
            insertedItem,
            default,
            default
        ) );

        return collection;
    }

    private static Mock<IMongoCollection<CatalogItem>> CreateMockIMongoCollectionDelete ( IAsyncCursor<CatalogItem> cursor, Func<CatalogItem, bool> filter )
    {
        var collection = new Mock<IMongoCollection<CatalogItem>> ( );
        var totalItems = new List<CatalogItem> ( );
        CatalogItem? deletedItem = null;

        cursor.ForEachAsync ( item =>
        {
            if ( filter ( item ) )
                deletedItem = item.CopyCatalogItem ( );
            else
                totalItems.Add ( item );
        } );

        var filteredCursor = CreateMockIAsyncCursor ( totalItems );

        collection.Setup ( x => x.FindAsync (
            It.IsAny<FilterDefinition<CatalogItem>> ( ),
            It.IsAny<FindOptions<CatalogItem>> ( ),
            default
        ) ).ReturnsAsync ( filteredCursor.Object );

        collection.Setup ( x => x.FindOneAndDeleteAsync<CatalogItem?> (
            It.IsAny<FilterDefinition<CatalogItem>> ( ),
            default,
            default
        ) ).ReturnsAsync ( deletedItem );



        return collection;
    }

    private static Mock<IMongoCollection<CatalogItem>> CreateMockIMongoCollectionReplace ( IAsyncCursor<CatalogItem> cursor, Func<CatalogItem, bool> filter, CatalogItem replacement )
    {
        var collection = new Mock<IMongoCollection<CatalogItem>> ( );
        CatalogItem? oldItem = null;
        CatalogItem? curatedItem = null;

        cursor.ForEachAsync ( item =>
        {
            if ( filter ( item ) )
            {
                oldItem = item;
            }
        } );

        var tempList = new List<CatalogItem> ( );

        if ( oldItem is not null )
        {
            tempList.Add ( oldItem );
            curatedItem = new ( )
            {
                Name = replacement.Name,
                Id = replacement.Id,
                CreatedDate = oldItem.CreatedDate,
                UpdatedDate = DateTime.UtcNow,
                Price = replacement.Price,
                Description = replacement.Description,
                ImageLocation = replacement.ImageLocation,
            };
        }

        var filteredCursor = CreateMockIAsyncCursor ( tempList );

        collection.Setup ( x => x.FindAsync (
            It.IsAny<FilterDefinition<CatalogItem>> ( ),
            It.IsAny<FindOptions<CatalogItem>> ( ),
            default
        ) ).ReturnsAsync ( filteredCursor.Object );

        collection.Setup ( funcCollection => funcCollection.FindOneAndReplaceAsync (
            It.IsAny<FilterDefinition<CatalogItem>> ( ),
            It.IsAny<CatalogItem> ( ),
            It.Is<FindOneAndReplaceOptions<CatalogItem, CatalogItem?>> (
                option => option.ReturnDocument == ReturnDocument.After
            ),
            default
        ) ).ReturnsAsync ( curatedItem );

        return collection;
    }

    private readonly string _databaseName = "catalog";
    private readonly string _collectionName = "items";

    [Fact]
    public async Task GetCatalogItemById_WithExistingOneItem_ReturnsOneItem ( )
    {
        // Arrange
        var product = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> { product };

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionFind ( cursor.Object, item => item.Id == product.Id );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        // Act
        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var result = await repo.GetCatalogItemById ( product.Id );

        Assert.Equal ( product, result );

    }

    [Fact]
    public async Task GetCatalogItemById_WithNonExistingItem_ReturnsNull ( )
    {
        var productId = Guid.NewGuid ( );
        // Arrange
        var productList = new List<CatalogItem> ( );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionFind ( cursor.Object, item => item.Id == productId );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        // Act
        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var result = await repo.GetCatalogItemById ( productId );

        Assert.Null ( result );

    }

    [Fact]
    public async Task GetCatalogItemById_WithExistingItems_ReturnsOneItem ( )
    {
        var product = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> { product };
        var count = 5;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionFind ( cursor.Object, item => item.Id == product.Id );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var result = await repo.GetCatalogItemById ( product.Id );

        Assert.Equal ( product, result );

    }

    [Fact]
    public async Task GetCatalogItemById_WithExistingItems_ReturnsNoItem ( )
    {
        var productId = Guid.NewGuid ( );
        var productList = new List<CatalogItem> ( );
        var count = 6;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionFind ( cursor.Object, item => item.Id == productId );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var result = await repo.GetCatalogItemById ( productId );

        Assert.Null ( result );

    }

    [Fact]
    public async Task GetCatalogItemById_WithExistingOneItem_ReturnsNoItem ( )
    {
        var productId = Guid.NewGuid ( );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> { product };

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionFind ( cursor.Object, item => item.Id == productId );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var result = await repo.GetCatalogItemById ( productId );

        Assert.Null ( result );

    }

    [Fact]
    public async Task GetAllCatalogItems_WithExistingItems_ReturnsAllItems ( )
    {
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionFind ( cursor.Object, _ => true );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( productList, result );

    }

    [Fact]
    public async Task GetAllCatalogItems_WithNoItems_ReturnsNoItems ( )
    {
        var productList = new List<CatalogItem> ( );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionFind ( cursor.Object, _ => true );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( result );

    }

    [Fact]
    public async Task CreateCatalogItem_WithOneItem_ReturnsAllItems ( )
    {
        var insertedProduct = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> ( );
        var expected = new List<CatalogItem> ( );
        var count = 6;

        for ( var i = 0; i < count; i++ )
        {
            var fakeModel = FakeModels.CreateFakeCatalogItem ( );
            productList.Add ( fakeModel );
            expected.Add ( fakeModel );
        }

        expected.Add ( insertedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionInsert ( cursor.Object, insertedProduct );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        await repo.CreateCatalogItem ( insertedProduct );
        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expected, result );

    }

    [Fact]
    public async Task CreateCatalogItem_WithNoItem_ReturnsOneItem ( )
    {
        var insertedProduct = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> ( );
        var expected = new List<CatalogItem> { insertedProduct };

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionInsert ( cursor.Object, insertedProduct );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        // Act
        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        await repo.CreateCatalogItem ( insertedProduct );
        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expected, result );

    }

    [Fact]
    public async Task UpdateCatalogItemPriceyId_WithExistingItems_ReturnsAllItemsWithModifiedItem ( )
    {
        var newPrice = RandomNumber.Next ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var expectedProduct = product.CopyCatalogItem ( );
        expectedProduct.UpdatedDate = newUpdateTime;
        expectedProduct.Price = newPrice;
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var expected = new List<CatalogItem> ( productList );

        productList.Add ( product );
        expected.Add ( expectedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = newPrice,
            Description = item.Description,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemPriceById ( newPrice, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expectedProduct, resultItem );
        Assert.Equal ( expected, result );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );

    }

    [Fact]
    public async Task UpdateCatalogItemPriceById_WithOneItem_ReturnsOneModifiedItem ( )
    {
        var newPrice = RandomNumber.Next ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var expectedProduct = product.CopyCatalogItem ( );
        expectedProduct.UpdatedDate = newUpdateTime;
        expectedProduct.Price = newPrice;
        var productList = new List<CatalogItem> ( );

        var expected = new List<CatalogItem> ( );

        productList.Add ( product );
        expected.Add ( expectedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = newPrice,
            Description = item.Description,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemPriceById ( newPrice, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expectedProduct, resultItem );
        Assert.Equal ( expected, result );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );

    }

    [Fact]
    public async Task UpdateCatalogItemPriceById_WithNoItems_ReturnsNull ( )
    {
        var newId = Guid.NewGuid ( );
        var newPrice = RandomNumber.Next ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var productList = new List<CatalogItem> ( );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == newId, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = newPrice,
            Description = item.Description,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemPriceById ( newPrice, newId );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( resultItem );
        Assert.Null ( result );
    }

    [Fact]
    public async Task UpdateCatalogItemPriceById_WithExistingItems_ReturnsNull ( )
    {
        var newPrice = RandomNumber.Next ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var expected = new List<CatalogItem> ( productList );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = newPrice,
            Description = item.Description,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemPriceById ( newPrice, product.Id );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( resultItem );
        Assert.Equal ( expected, result );

    }

    [Fact]
    public async Task UpdateCatalogItemNameById_WithExistingItems_ReturnsAllItemsWithModifiedItem ( )
    {
        var newName = Name.First ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var expectedProduct = product.CopyCatalogItem ( );
        expectedProduct.UpdatedDate = newUpdateTime;
        expectedProduct.Name = newName;
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var expected = new List<CatalogItem> ( productList );

        productList.Add ( product );
        expected.Add ( expectedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = newName,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = item.Description,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemNameById ( newName, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expectedProduct, resultItem );
        Assert.Equal ( expected, result );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );

    }

    [Fact]
    public async Task UpdateCatalogItemNameById_WithOneItem_ReturnsOneModifiedItem ( )
    {
        var newName = Name.First ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var expectedProduct = product.CopyCatalogItem ( );
        expectedProduct.UpdatedDate = newUpdateTime;
        expectedProduct.Name = newName;
        var productList = new List<CatalogItem> ( );

        var expected = new List<CatalogItem> ( );

        productList.Add ( product );
        expected.Add ( expectedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = newName,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = item.Description,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemNameById ( newName, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expectedProduct, resultItem );
        Assert.Equal ( expected, result );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );

    }

    [Fact]
    public async Task UpdateCatalogItemNameById_WithNoItems_ReturnsNull ( )
    {
        var newId = Guid.NewGuid ( );
        var newName = Name.First ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var productList = new List<CatalogItem> ( );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == newId, item => new ( )
        {
            Name = newName,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = item.Description,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemNameById ( newName, newId );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( resultItem );
        Assert.Null ( result );
    }

    [Fact]
    public async Task UpdateCatalogItemNameById_WithExistingItems_ReturnsNull ( )
    {
        var newName = Name.First ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var expected = new List<CatalogItem> ( productList );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = newName,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = item.Description,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemNameById ( newName, product.Id );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( resultItem );
        Assert.Equal ( expected, result );

    }

    [Fact]
    public async Task UpdateCatalogItemDescriptionById_WithExistingItems_ReturnsAllItemsWithModifiedItem ( )
    {
        var newDescription = Lorem.Paragraph ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var expectedProduct = product.CopyCatalogItem ( );
        expectedProduct.UpdatedDate = newUpdateTime;
        expectedProduct.Description = newDescription;
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var expected = new List<CatalogItem> ( productList );

        productList.Add ( product );
        expected.Add ( expectedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = newDescription,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemDescriptionById ( newDescription, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expectedProduct, resultItem );
        Assert.Equal ( expected, result );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );

    }

    [Fact]
    public async Task UpdateCatalogItemDescriptionById_WithOneItem_ReturnsOneModifiedItem ( )
    {
        var newDescription = Lorem.Paragraph ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var expectedProduct = product.CopyCatalogItem ( );
        expectedProduct.UpdatedDate = newUpdateTime;
        expectedProduct.Description = newDescription;
        var productList = new List<CatalogItem> ( );

        var expected = new List<CatalogItem> ( );

        productList.Add ( product );
        expected.Add ( expectedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = newDescription,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemDescriptionById ( newDescription, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expectedProduct, resultItem );
        Assert.Equal ( expected, result );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );

    }

    [Fact]
    public async Task UpdateCatalogItemDescriptionById_WithNoItems_ReturnsNull ( )
    {
        var newId = Guid.NewGuid ( );
        var newDescription = Lorem.Paragraph ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var productList = new List<CatalogItem> ( );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == newId, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = newDescription,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemNameById ( newDescription, newId );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( resultItem );
        Assert.Null ( result );
    }

    [Fact]
    public async Task UpdateCatalogItemDescriptionById_WithExistingItems_ReturnsNull ( )
    {
        var newDescription = Lorem.Paragraph ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var expected = new List<CatalogItem> ( productList );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = newDescription,
            ImageLocation = item.ImageLocation
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemNameById ( newDescription, product.Id );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( resultItem );
        Assert.Equal ( expected, result );

    }

    [Fact]
    public async Task UpdateCatalogItemImageById_WithExistingItems_ReturnsAllItemsWithModifiedItem ( )
    {
        var newImageAddress = FakeModels.CreateManyImageLocations ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var expectedProduct = product.CopyCatalogItem ( );
        expectedProduct.UpdatedDate = newUpdateTime;
        expectedProduct.ImageLocation = newImageAddress;
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var expected = new List<CatalogItem> ( productList );

        productList.Add ( product );
        expected.Add ( expectedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = item.Description,
            ImageLocation = newImageAddress,
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemImageById ( newImageAddress, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expectedProduct, resultItem );
        Assert.Equal ( expected, result );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );

    }

    [Fact]
    public async Task UpdateCatalogItemImageById_WithOneItem_ReturnsOneModifiedItem ( )
    {
        var newImageAddress = FakeModels.CreateManyImageLocations ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var expectedProduct = product.CopyCatalogItem ( );
        expectedProduct.UpdatedDate = newUpdateTime;
        expectedProduct.ImageLocation = newImageAddress;
        var productList = new List<CatalogItem> ( );

        var expected = new List<CatalogItem> ( );

        productList.Add ( product );
        expected.Add ( expectedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = item.Description,
            ImageLocation = newImageAddress,
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemImageById ( newImageAddress, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expectedProduct, resultItem );
        Assert.Equal ( expected, result );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );

    }

    [Fact]
    public async Task UpdateCatalogItemImageById_WithNoItems_ReturnsNull ( )
    {
        var newId = Guid.NewGuid ( );
        var newImageAddress = FakeModels.CreateManyImageLocations ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var productList = new List<CatalogItem> ( );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == newId, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = item.Description,
            ImageLocation = newImageAddress,
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemImageById ( newImageAddress, newId );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( resultItem );
        Assert.Null ( result );
    }

    [Fact]
    public async Task UpdateCatalogItemImageById_WithExistingItems_ReturnsNull ( )
    {
        var newImageAddress = FakeModels.CreateManyImageLocations ( );
        var newUpdateTime = DateTime.UtcNow.AddSeconds ( RandomNumber.Next ( 0, 10 ) );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        var expected = new List<CatalogItem> ( productList );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionUpdate ( cursor.Object, item => item.Id == product.Id, item => new ( )
        {
            Name = item.Name,
            Id = item.Id,
            CreatedDate = item.CreatedDate,
            UpdatedDate = newUpdateTime,
            Price = item.Price,
            Description = item.Description,
            ImageLocation = newImageAddress,
        } );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.UpdateCatalogItemImageById ( newImageAddress, product.Id );

        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( resultItem );
        Assert.Equal ( expected, result );

    }

    [Fact]
    public async Task ReplaceCatalogItemById_WithExistingItems_ReturnsAllItemsWithModifiedItem ( )
    {
        var product = FakeModels.CreateFakeCatalogItem ( );
        var newItem = FakeModels.CreateFakeCatalogItemById ( product.Id );
        var productList = new List<CatalogItem> ( );
        var count = 10;

        for ( var i = 0; i < count; i++ )
        {
            productList.Add ( FakeModels.CreateFakeCatalogItem ( ) );
        }

        productList.Add ( product );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionReplace ( cursor.Object, item => item.Id == product.Id, newItem );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.ReplaceCatalogItemById ( newItem, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        Assert.Equal ( newItem.Name, resultItem.Name );
        Assert.Equal ( newItem.Id, resultItem.Id );
        Assert.Equal ( product.CreatedDate, resultItem.CreatedDate );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );
        Assert.Equal ( newItem.Price, resultItem.Price );
        Assert.Equal ( newItem.Description, resultItem.Description );
        Assert.Equal ( newItem.ImageLocation, resultItem.ImageLocation );

    }

    [Fact]
    public async Task ReplaceCatalogItemById_WithOneItem_ReturnsOneModifiedItem ( )
    {
        var product = FakeModels.CreateFakeCatalogItem ( );
        var newItem = FakeModels.CreateFakeCatalogItemById ( product.Id );
        var productList = new List<CatalogItem> { product };

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionReplace ( cursor.Object, item => item.Id == product.Id, newItem );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.ReplaceCatalogItemById ( newItem, product.Id );

        if ( resultItem is null )
        {
            throw new ArgumentNullException ( );
        }

        Assert.Equal ( newItem.Name, resultItem.Name );
        Assert.Equal ( newItem.Id, resultItem.Id );
        Assert.Equal ( product.CreatedDate, resultItem.CreatedDate );
        Assert.NotEqual ( product.UpdatedDate, resultItem.UpdatedDate );
        Assert.Equal ( newItem.Price, resultItem.Price );
        Assert.Equal ( newItem.Description, resultItem.Description );
        Assert.Equal ( newItem.ImageLocation, resultItem.ImageLocation );

    }

    [Fact]
    public async Task ReplaceCatalogItemById_WithNoItems_ReturnsNull ( )
    {
        var product = FakeModels.CreateFakeCatalogItem ( );
        var newItem = FakeModels.CreateFakeCatalogItemById ( product.Id );
        var productList = new List<CatalogItem> ( );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionReplace ( cursor.Object, item => item.Id == product.Id, newItem );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.ReplaceCatalogItemById ( newItem, product.Id );

        Assert.Null ( resultItem );
    }

    [Fact]
    public async Task ReplaceCatalogItemById_WithExistingItems_ReturnsNull ( )
    {
        var fakeProduct = FakeModels.CreateFakeCatalogItem ( );
        var product = FakeModels.CreateFakeCatalogItem ( );
        var newItem = FakeModels.CreateFakeCatalogItemById ( product.Id );
        var productList = new List<CatalogItem> { fakeProduct };

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionReplace ( cursor.Object, item => item.Id == product.Id, newItem );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultItem = await repo.ReplaceCatalogItemById ( newItem, product.Id );

        Assert.Null ( resultItem );

    }

    [Fact]
    public async Task DeleteCatalogItemById_WithOneItem_ReturnsAllItems ( )
    {
        var deletedProduct = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> ( );
        bool Filter ( CatalogItem item ) => item.Id == deletedProduct.Id;
        var count = 6;

        for ( var i = 0; i < count; i++ )
        {
            var fakeModel = FakeModels.CreateFakeCatalogItem ( );
            productList.Add ( fakeModel );
        }
        productList.Add ( deletedProduct );

        var expected = new List<CatalogItem> ( productList ).Where ( item => !Filter ( item ) ).ToList ( );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionDelete ( cursor.Object, Filter );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultDeleteItem = await repo.DeleteCatalogItemById ( deletedProduct.Id );
        var result = await repo.GetAllCatalogItems ( );

        Assert.Equal ( expected, result );
        Assert.Equal ( deletedProduct, resultDeleteItem );

    }

    [Fact]
    public async Task DeleteCatalogItemById_WithOneItem_ReturnsOneItem ( )
    {
        var deletedProduct = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> ( );
        bool Filter ( CatalogItem item ) => item.Id == deletedProduct.Id;

        productList.Add ( deletedProduct );

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionDelete ( cursor.Object, Filter );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultDeleteItem = await repo.DeleteCatalogItemById ( deletedProduct.Id );
        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( result );
        Assert.Equal ( deletedProduct, resultDeleteItem );

    }

    [Fact]
    public async Task DeleteCatalogItemById_WithNoItem_ReturnsNoItem ( )
    {
        var deletedProduct = FakeModels.CreateFakeCatalogItem ( );
        var productList = new List<CatalogItem> ( );
        bool Filter ( CatalogItem item ) => item.Id == deletedProduct.Id;

        var cursor = CreateMockIAsyncCursor ( productList );
        var collection = CreateMockIMongoCollectionDelete ( cursor.Object, Filter );
        var mongoFactory = CreateMockIMongoDbFactory ( collection.Object );

        var repo = new MongoDbCatalogItemRepository ( mongoFactory.Object );

        var resultDeleteItem = await repo.DeleteCatalogItemById ( deletedProduct.Id );
        var result = await repo.GetAllCatalogItems ( );

        Assert.Null ( result );
        Assert.Null ( resultDeleteItem );

    }
}