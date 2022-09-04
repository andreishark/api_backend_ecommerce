namespace api_service.Controllers;

using api_service.ApiConfiguration;
using DatabaseLibrary;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.CreateDto;
using Models.GetDto;

[ApiController]
[Route ( "api/v1/config" )]
public class ConfigSettingsController : ControllerBase
{
    private readonly ILogger<CatalogController> _logger;
    private readonly IApiConfiguration _config;

    public ConfigSettingsController ( ILogger<CatalogController> logger, IApiConfiguration config )
    {
        _logger = logger;
        _config = config;

        _logger.LogInformation ( "CatalogController initialized" );
    }

    [HttpGet ( "version" )]
    public ActionResult<string> GetVersion ( )
    {
        _logger.LogInformation ( "Getting version" );
        return Ok ( _config.GetApiVersion ( ) );
    }
}