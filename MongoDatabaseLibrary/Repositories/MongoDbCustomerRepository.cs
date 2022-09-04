using DatabaseLibrary;
using Models.models;
using MongoDatabaseLibrary.Factories;
using MongoDB.Driver;

namespace MongoDatabaseLibrary.Repositories;

public class MongoDbCustomerRepository : ICustomersRepository
    {
    private readonly IMongoCollection<Customer> _customerCollection;
    private const string databaseName = "customers";
    private const string collectionName = "profiles";

    public MongoDbCustomerRepository ( IMongoDbFactory mongoFactory )
        {
        _customerCollection = mongoFactory.GetCollection<Customer> ( databaseName, collectionName );
        }

    public Customer CreateCustomer ( Customer customer )
        {
        _customerCollection.InsertOne ( customer );
        return customer;
        }

    public Customer GetCustomerById ( Guid customerId )
        {
        throw new NotImplementedException ( );
        }

    public IEnumerable<Customer> GetCustomers ( )
        {
        throw new NotImplementedException ( );
        }
    }