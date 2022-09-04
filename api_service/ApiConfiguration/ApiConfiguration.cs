using System.ComponentModel.DataAnnotations;

namespace api_service.ApiConfiguration;

public class ApiConfiguration : IApiConfiguration
{

    private readonly SettingsMap _settings;

    public ApiConfiguration ( SettingsMap settings )
    {
        _settings = settings;
    }

    public string GetApiVersion ( )
    {
        return _settings.Version;
    }
}