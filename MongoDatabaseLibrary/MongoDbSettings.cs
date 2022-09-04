namespace MongoDatabaseLibrary;

public class MongoDbSettings
    {
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConnectionString
        {
        get
            {
            return $"mongodb://{User}:{Password}@{Host}:{Port}";
            }
        }
    }