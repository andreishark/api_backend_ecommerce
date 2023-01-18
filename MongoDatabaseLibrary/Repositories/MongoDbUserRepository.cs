using DatabaseLibrary;
using Models.CreateDto;
using Models.models;
using MongoDatabaseLibrary.Factories;
using MongoDB.Driver;

namespace MongoDatabaseLibrary.Repositories;

public class MongoDbUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _usersCollection;
    private const string DatabaseName = "Users";
    private const string CollectionName = "UsersCredentials";
    private readonly FilterDefinitionBuilder<User> _filterBuilderCatalogItem = Builders<User>.Filter;

    public MongoDbUserRepository(IMongoDbFactory mongoDbFactory)
    {
        _usersCollection = mongoDbFactory.GetCollection<User>(DatabaseName, CollectionName);
    }


    public async Task<User> GetUserById(Guid id)
    {
        var filter = _filterBuilderCatalogItem.Eq(user => user.Id, id);
        return await _usersCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User> GetUserByLoginCredentials(UserLoginDto userLogin)
    {
        var filter = _filterBuilderCatalogItem.Eq(user => user.Username,
                         userLogin.Username) &
                     _filterBuilderCatalogItem.Eq(user => user.Password,
                         userLogin.Password);
        return await _usersCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User> CreateUser(User user)
    {
        await _usersCollection.InsertOneAsync(user);
        return user;
    }

    public async Task<User> GetUserByEmail(string email)
    {
        var filter = _filterBuilderCatalogItem.Eq(user => user.EmailAddress, email);
        return await _usersCollection.Find(filter).FirstOrDefaultAsync();
    }
}