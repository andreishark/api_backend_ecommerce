using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDatabaseLibrary.Factories;

public class CustomMongoDbFactory : IMongoDbFactory
{
    private readonly IMongoClient _client;

    public CustomMongoDbFactory ( MongoDbSettings mongoSettings )
    {
        var settings = MongoClientSettings.FromConnectionString ( mongoSettings.ConnectionString );
        settings.ServerApi = new ServerApi ( ServerApiVersion.V1 );
        settings.LinqProvider = LinqProvider.V3;

        _client = new MongoClient ( settings );
    }

    public IMongoCollection<T> GetArchiveCollection<T> ( string databaseName )
    {
        return _client.GetDatabase ( databaseName ).GetCollection<T> ( IMongoDbFactory.archiveCollectionName );
    }

    public IMongoCollection<T> GetCollection<T> ( string databaseName, string collectionName )
    {
        return _client.GetDatabase ( databaseName ).GetCollection<T> ( collectionName );
    }
}