using MongoDB.Driver;

namespace MongoDatabaseLibrary.Factories;

public interface IMongoDbFactory
{
    public const string ArchiveCollectionName = "archive";

    IMongoCollection<T> GetCollection<T> ( string databaseName, string collectionName );

    IMongoCollection<T> GetArchiveCollection<T> ( string databaseName );
}