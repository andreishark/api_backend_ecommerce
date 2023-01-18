using Models.CreateDto;
using Models.models;

namespace DatabaseLibrary;

public interface IUserRepository
{
    public Task<User> GetUserById(Guid id);

    public Task<User> GetUserByLoginCredentials(UserLoginDto userLogin);

    public Task<User> CreateUser(User user);

    public Task<User> GetUserByEmail(string email);
}