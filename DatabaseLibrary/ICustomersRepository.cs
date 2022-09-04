using Models.models;

namespace DatabaseLibrary;

public interface ICustomersRepository
{
    public IEnumerable<Customer> GetCustomers();

    public Customer GetCustomerById(Guid customerId);

    public Customer CreateCustomer(Customer customer);
}