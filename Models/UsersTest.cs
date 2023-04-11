using Models.models;

namespace Models;

public class UsersTest
{
    public static List<User> Users = new List<User>()
    {
        new User()
        {
            Username = "andreishark",
            Password = "123456",
            EmailAddress = "andreishark10@gmail.com",
            Role = "Admin"
        }
    };
}