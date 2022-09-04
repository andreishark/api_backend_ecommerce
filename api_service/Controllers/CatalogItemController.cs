using api_service.ApiConfiguration;
using DatabaseLibrary;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.CreateDto;
using Models.GetDto;

namespace api_service.Controllers;

[ApiController]
[Route ( "api/catalog" )]
public class CatalogController : ControllerBase
{
    private readonly ILogger<CatalogController> _logger;
    private readonly ICatalogItemRepository _catalogItemRepository;
    private readonly IApiConfiguration _config;
    private readonly string staticImagesPath = "static/images/CatalogItems";

    public CatalogController ( ILogger<CatalogController> logger, ICatalogItemRepository customersRepository, IApiConfiguration config )
    {
        _logger = logger;
        _catalogItemRepository = customersRepository;
        _config = config;

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

        // IFormFile? file;
        // try
        // {
        //     file = Request.Form.Files.SingleOrDefault ( );
        // }
        // catch ( InvalidOperationException e )
        // {
        //     _logger.LogError ( "Could not read form files: {}", e.Message );
        //     return Problem ( "Could not read form files" );
        // }

        // if ( file is null )
        // {
        //     _logger.LogWarning ( "Product with name {} doesn't have an image", product.Name );
        //     return BadRequest ( ModelState );
        // }

        _logger.LogInformation ( "Verifying model" );
        if ( !ModelState.IsValid )
        {
            _logger.LogWarning ( "Model invalid, not uploading product with name {}", product.Name );
            return BadRequest ( ModelState );
        }

        _logger.LogInformation ( "Creating product with name {}", product.Name );
        var createdProduct = await _catalogItemRepository.CreateCatalogItem ( product.AsCatalogItem ( ) );

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

        CatalogItemCreateDto oldItemCreateDto = oldItem.AsCreateDto ( );

        patchDoc.ApplyTo ( oldItemCreateDto );

        var newItem = oldItemCreateDto.AsCatalogItem ( );

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
