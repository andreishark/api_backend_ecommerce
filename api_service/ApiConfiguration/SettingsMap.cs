using System.ComponentModel.DataAnnotations;

namespace api_service.ApiConfiguration;

public class SettingsMap
{
    public static readonly string Path = "ApiConfiguration";

    [Required]
    public string Version { get; set; } = "Not found";
}