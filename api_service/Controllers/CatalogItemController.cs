using api_service.ApiConfiguration;
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
    public async Task<ActionResult<CatalogItemGetDto>> InsertCatalogItem ( [FromBody] CatalogItemCreateDto product )
    {

        _logger.LogInformation ( "Getting product image" );

        IFormFile? file;
        try
        {
            file = Request.Form.Files.SingleOrDefault ( );
        }
        catch ( InvalidOperationException ex )
        {
            _logger.LogError ( "Could not read form files: {}", ex.Message );
            _logger.LogTrace ( ex.StackTrace );
            return Problem ( "Could not read form files" );
        }

        if ( file is null || file.Length == 0 )
        {
            _logger.LogWarning ( "Product with name {} doesn't have an image", product.Name );
            return BadRequest ( ModelState );
        }

        _logger.LogInformation ( "Verifying model" );
        if ( !ModelState.IsValid )
        {
            _logger.LogWarning ( "Model invalid, not uploading product with name {}", product.Name );
            return BadRequest ( ModelState );
        }

        _logger.LogInformation ( "Generating Guid" );
        var product_id = Guid.NewGuid ( );
        var image_id = Guid.NewGuid ( );

        _logger.LogInformation ( "Creating apiPath for image" );
        var apiPath = Path.Combine ( staticImagesPathApi, product_id.ToString ( ), image_id.ToString ( ) );

        _logger.LogInformation ( "Trying to upload image to disk" );
        try
        {
            string filePath = Path.Combine ( staticImagesPathDisk, product_id.ToString ( ), image_id.ToString ( ) );

            using Stream fileStream = new FileStream ( filePath, FileMode.Create );
            await file.CopyToAsync ( fileStream );
        }
        catch ( System.Exception ex )
        {
            _logger.LogError ( "File couldn't be written to disk.\nError: {}", ex.Message );
            _logger.LogTrace ( ex.StackTrace );
            return Problem ( "File couldn't be written to disk" );
        }

        _logger.LogInformation ( "File uploaded successfully." );

        _logger.LogInformation ( "Creating product with name {}", product.Name );
        var createdProduct = await _catalogItemRepository.CreateCatalogItem ( product.AsCatalogItem ( apiPath, product_id ) );

        if ( createdProduct == null )
        {
            _logger.LogWarning ( "Product with name {} could not be created", product.Name );
            return BadRequest ( ModelState );
        }

        _logger.LogInformation ( "Product with name {} created successfully", product.Name );
        return Ok ( createdProduct.AsGetDto ( ) );
    }

    [HttpPatch ( "update_item/{id}" )]
    public async Task<ActionResult<CatalogItemGetDto>> UpdateCatalogItem ( Guid id, [FromBody] JsonPatchDocument<CatalogItemCreateDto> patchDoc )
    {
        if ( patchDoc is null )
        {
            _logger.LogWarning ( "PatchDoc is null" );
            return BadRequest ( ModelState );
        }
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

        string? apiPath = null;
        _logger.LogInformation ( "Getting product image" );

        IFormFile? file;
        try
        {
            file = Request.Form.Files.SingleOrDefault ( );

            if ( file is null || file.Length == 0 )
            {
                _logger.LogWarning ( "Product wasn't appended to request" );
                return BadRequest ( ModelState );
            }

            _logger.LogInformation ( "Generating Guid" );
            var image_id = Guid.NewGuid ( );

            _logger.LogInformation ( "Creating apiPath for image" );
            apiPath = Path.Combine ( staticImagesPathApi, oldItem.Id.ToString ( ), image_id.ToString ( ) );

            _logger.LogInformation ( "Trying to upload image to disk" );
            try
            {
                string filePath = Path.Combine ( staticImagesPathDisk, oldItem.Id.ToString ( ), image_id.ToString ( ) );

                using Stream fileStream = new FileStream ( filePath, FileMode.Create );
                await file.CopyToAsync ( fileStream );
            }
            catch ( System.Exception ex )
            {
                _logger.LogError ( "File couldn't be written to disk.\nError: {}", ex.Message );
                _logger.LogTrace ( ex.StackTrace );
                return Problem ( "File couldn't be written to disk" );
            }
        }
        catch ( InvalidOperationException ex )
        {
            _logger.LogWarning ( "Could not read form files: {}, skipping image check", ex.Message );
        }


        CatalogItemCreateDto oldItemCreateDto = oldItem.AsCreateDto ( );

        patchDoc.ApplyTo ( oldItemCreateDto );

        CatalogItem? newItem;

        if ( apiPath is null )
            newItem = oldItemCreateDto.AsCatalogItem ( oldItem.ImageLocation, oldItem.Id );
        else
            newItem = oldItemCreateDto.AsCatalogItem ( apiPath, oldItem.Id );

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
