using api_service.ApiConfiguration;
using api_service.Utility;
using DatabaseLibrary;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.CreateDto;
using Models.GetDto;
using Models.models;

namespace api_service.Controllers;

[ApiController]
[Route ( "api/catalog" )]
public class CatalogController : ControllerBase
{
    private readonly ILogger<CatalogController> _logger;
    private readonly ICatalogItemRepository _catalogItemRepository;
    private readonly IApiConfiguration _config;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly string staticImagesPathApi = "static/images/CatalogItems";
    private readonly string staticImagesPathDisk;

    public CatalogController (
        ILogger<CatalogController> logger,
        ICatalogItemRepository customersRepository,
        IApiConfiguration config,
        IWebHostEnvironment hostEnvironment )
    {
        _logger = logger;
        _catalogItemRepository = customersRepository;
        _config = config;
        _hostEnvironment = hostEnvironment;

        staticImagesPathDisk = Path.Combine ( _hostEnvironment.ContentRootPath, "Static/images/CatalogItems" );

        _logger.LogInformation ( "CatalogController initialized" );
    }



    [HttpGet ( "get_products" )]
    public async Task<ActionResult<IEnumerable<CatalogItemGetDto>>> GetCatalogItems ( )
    {
        _logger.LogInformation ( "Getting items" );
        var items = await _catalogItemRepository.GetAllCatalogItems ( );

        if ( items is null )
        {
            _logger.LogWarning ( "Items not found" );
            return NotFound ( "There aren't any items in the catalog" );
        }

        _logger.LogInformation ( "Items found, returning" );
        return Ok ( items.Select ( item => item.AsGetDto ( ) ) );
    }

    [HttpGet ( "get_product/{id}" )]
    public async Task<ActionResult<CatalogItemGetDto>> GetCatalogItemById ( Guid id )
    {
        _logger.LogInformation ( "Getting item with id {}", id );
        var item = await _catalogItemRepository.GetCatalogItemById ( id );

        if ( item is null )
        {
            _logger.LogWarning ( "Item with id {} could not be found", id );
            return NotFound ( );
        }

        _logger.LogInformation ( "Product with id {} found", id );
        return Ok ( item );
    }

    [HttpPost ( "insert_product" )]
    public async Task<ActionResult<CatalogItemGetDto>> InsertCatalogItem ( [FromForm] CatalogItemCreateDto product )
    {

        _logger.LogInformation ( "Getting product images" );

        IFormFile [ ]? files;
        try
        {
            files = Request.Form.Files.ToArray ( );
        }
        catch ( InvalidOperationException ex )
        {
            _logger.LogError ( "Could not read form files: {}", ex.Message );
            _logger.LogTrace ( ex.StackTrace );
            return Problem ( "Could not read form files" );
        }

        if ( files is null || files.Length == 0 )
        {
            _logger.LogWarning ( "Product with name {} doesn't have an image", product.Name );
            return BadRequest ( "Product doesn't have an image" );
        }

        _logger.LogInformation ( "Verifying model" );
        if ( !ModelState.IsValid )
        {
            _logger.LogWarning ( "Model invalid, not uploading product with name {}", product.Name );
            return BadRequest ( ModelState );
        }

        _logger.LogInformation ( "Generating Guid" );
        var productId = Guid.NewGuid ( );

        var apiPaths = await ImageManipulation.WriteImagesToDiskAsync ( staticImagesPathDisk, staticImagesPathApi, _logger, files, productId );

        if ( apiPaths is null )
        {
            return Problem ( "There were problems in creating the files" );
        }

        _logger.LogInformation ( "Creating product with name {}", product.Name );
        var createdProduct = await _catalogItemRepository.CreateCatalogItem ( product.AsCatalogItem ( apiPaths, productId ) );

        if ( createdProduct == null )
        {
            _logger.LogWarning ( "Product with name {} could not be created", product.Name );
            return BadRequest ( ModelState );
        }

        _logger.LogInformation ( "Product with name {} created successfully", product.Name );
        return Ok ( createdProduct.AsGetDto ( ) );
    }

    [HttpPatch ( "update_item/{id}/{boolAddImage}" )]
    public async Task<ActionResult<CatalogItemGetDto>> UpdateCatalogItem ( bool boolAddImage, Guid id, [FromForm] JsonPatchDocument<CatalogItemCreateDto> patchDoc )
    {
        _logger.LogInformation ( "PatchDoc is not null" );

        if ( !ModelState.IsValid )
        {
            _logger.LogWarning ( "PatchDoc is not valid" );
            return BadRequest ( ModelState );
        }
        _logger.LogInformation ( "PatchDoc is valid" );

        var oldItem = await _catalogItemRepository.GetCatalogItemById ( id );

        if ( oldItem is null )
        {
            _logger.LogWarning ( "Item with id {} doesn't exist", id );
            return BadRequest ( "Item doesn't exist" );
        }
        _logger.LogInformation ( "Item with id {} found", id );

        IFormFile [ ]? files;
        List<string> apiPaths = oldItem.ImageLocation;
        try
        {
            files = Request.Form.Files.ToArray ( );
        }
        catch ( InvalidOperationException ex )
        {
            _logger.LogError ( "Could not read form files: {}", ex.Message );
            _logger.LogTrace ( ex.StackTrace );
            return Problem ( "Could not read form files" );
        }

        if ( files.Length == 0 )
        {
            _logger.LogWarning ( "There were no images passed, skipping" );
        }
        else if ( boolAddImage )
        {
            var newApiPaths = await ImageManipulation.WriteImagesToDiskAsync ( staticImagesPathDisk, staticImagesPathApi, _logger, files, oldItem.Id );

            if ( newApiPaths is null )
            {
                _logger.LogCritical ( "Could not write images to disk" );
                return Problem ( "Images couldn't be written to disk" );
            }

            apiPaths.AddRange ( newApiPaths );
        }
        else if ( !boolAddImage )
        {
            List<string> pathFiles;
            try
            {
                pathFiles = files.Select ( item =>
                {
                    var itemName = item.FileName;

                    var filePath = apiPaths.Where ( path => path.Contains ( itemName ) ).SingleOrDefault ( );

                    if ( filePath is null )
                    {
                        _logger.LogCritical ( "Code violations" );
                        throw new NullReferenceException ( );
                    }

                    return filePath;
                } ).ToList ( );
            }
            catch ( NullReferenceException )
            {
                return Problem ( "Unexpected problem occurred" );
            }

            var temp = ImageManipulation.DeleteImagesFromDisk ( staticImagesPathDisk, _logger, pathFiles, apiPaths, id );

            if ( temp is null )
            {
                _logger.LogError ( "Couldn't delete images from disk" );
                return Problem ( "Couldn't delete images from disk" );
            }

            apiPaths = temp;
        }
        else
        {
            _logger.LogCritical ( "Code violations were detected" );
            return Problem ( "An unexpected problem occurred" );
        }

        var oldItemCreateDto = oldItem.AsCreateDto ( );

        patchDoc.ApplyTo ( oldItemCreateDto );

        CatalogItem? newItem;

        newItem = oldItemCreateDto.AsCatalogItem ( apiPaths, oldItem.Id, oldItem.CreatedDate );

        _logger.LogInformation ( "Replacing item with id {}", id );
        var returnedItem = await _catalogItemRepository.ReplaceCatalogItemById ( newItem, id );
        _logger.LogInformation ( "Replacement with item {} was successfully applied", id );

        return Ok ( returnedItem );

    }

    [HttpDelete ( "delete_item/{id}" )]
    public async Task<ActionResult<CatalogItemGetDto>> DeleteCatalogItem ( Guid id )
    {
        _logger.LogInformation ( "Deleting item with id {}", id );
        var item = await _catalogItemRepository.DeleteCatalogItemById ( id );

        if ( item is null )
        {
            _logger.LogWarning ( "Item with id {} was not found", id );
            return BadRequest ( ModelState );
        }

        _logger.LogInformation ( "Item with id {} was successfully deleted and archived", id );
        return Ok ( item );
    }
}
