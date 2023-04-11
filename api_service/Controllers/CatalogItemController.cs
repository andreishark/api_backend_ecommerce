using api_service.ApiConfiguration;
using api_service.Utility;
using DatabaseLibrary;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.CreateDto;
using Models.GetDto;
using Models.models;
using InvalidOperationException = System.InvalidOperationException;

namespace api_service.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly ILogger<CatalogController> _logger;
    private readonly ICatalogItemRepository _catalogItemRepository;
    private readonly IApiConfiguration _config;
    private const string StaticImagesPathApi = "static/images/CatalogItems";
    private readonly string _staticImagesPathDisk;

    public CatalogController(
        ILogger<CatalogController> logger,
        ICatalogItemRepository customersRepository,
        IApiConfiguration config,
        IHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _catalogItemRepository = customersRepository;
        _config = config;

        _staticImagesPathDisk = Path.Combine(hostEnvironment.ContentRootPath, "Static/images/CatalogItems");

        _logger.LogInformation("CatalogController initialized");
    }

    private (List<string> pathFiles, ActionResult<CatalogItemGetDto> problem) GetImageNamesFromFiles(
        IEnumerable<IFormFile> files, IReadOnlyCollection<string> apiPaths, Guid itemId)
    {
        List<string> pathFiles = files.Select(item => item.FileName)
            .Select(itemName =>
                apiPaths.SingleOrDefault(path => path.Equals($"{StaticImagesPathApi}/{itemId}/{itemName}")))
            .Where(filePath => filePath != null)
            .ToList()!;

        return (pathFiles, Ok());
    }

    private async Task<(List<string> apiPaths, ActionResult<CatalogItemGetDto> problem)> HandleImageUpdateCatalogItem(
        Guid id, bool boolAddImage, IReadOnlyCollection<IFormFile> files, CatalogItem oldItem,
        List<string> apiPaths)
    {
        ActionResult<CatalogItemGetDto> problem = Ok();
        if (files.Count != 0)
        {
            (apiPaths, var imageProblem) =
                await HandleImageCreationAndDeletion(id, boolAddImage, files, oldItem, apiPaths);
            if (imageProblem.Result is not OkResult)
                problem = imageProblem;
        }
        else
            _logger.LogWarning("There were no images passed, skipping");

        return (apiPaths, problem);
    }

    private async Task<(List<string> apiPaths, ActionResult<CatalogItemGetDto> problem)> HandleImageCreationAndDeletion(
        Guid id, bool boolAddImage, IEnumerable<IFormFile> files, CatalogItem oldItem,
        List<string> apiPaths)
    {
        switch (boolAddImage)
        {
            case true:
            {
                var newApiPaths = await ImageManipulation.WriteImagesToDiskAsync(_staticImagesPathDisk,
                    StaticImagesPathApi,
                    _logger,
                    files,
                    oldItem.Id);

                if (newApiPaths is null)
                {
                    _logger.LogCritical("Could not write images to disk");
                    return (apiPaths, Problem("Images couldn't be written to disk"));
                }

                apiPaths.AddRange(newApiPaths);
                break;
            }
            case false:
            {
                var (pathFiles, actionResult) = GetImageNamesFromFiles(files, apiPaths, id);
                if (actionResult.Result is not OkResult)
                {
                    return (apiPaths, actionResult);
                }

                var temp = ImageManipulation.DeleteImagesFromDisk(_staticImagesPathDisk,
                    _logger,
                    pathFiles,
                    apiPaths,
                    id);

                if (temp is null)
                {
                    _logger.LogError("Couldn't delete images from disk");
                    return (apiPaths, Problem("Couldn't delete images from disk"));
                }

                apiPaths = temp;
                break;
            }
        }

        return (apiPaths, Ok());
    }


    [HttpGet("get_products")]
    public async Task<ActionResult<IEnumerable<CatalogItemGetDto>>> GetCatalogItems()
    {
        _logger.LogInformation("Getting items");
        var items = await _catalogItemRepository.GetAllCatalogItems();

        if (items is null)
        {
            _logger.LogWarning("Items not found");
            return NotFound("There aren't any items in the catalog");
        }

        _logger.LogInformation("Items found, returning");
        return Ok(items.Select(item => item.AsGetDto()));
    }

    [HttpGet("get_product/{id:guid}")]
    public async Task<ActionResult<CatalogItemGetDto>> GetCatalogItemById(Guid id)
    {
        _logger.LogInformation("Getting item with id {}", id);
        var item = await _catalogItemRepository.GetCatalogItemById(id);

        if (item is null)
        {
            _logger.LogWarning("Item with id {} could not be found", id);
            return NotFound();
        }

        _logger.LogInformation("Product with id {} found", id);
        return Ok(item);
    }

    [HttpPost("insert_product")]
    public async Task<ActionResult<CatalogItemGetDto>> InsertCatalogItem([FromForm] CatalogItemCreateDto product)
    {
        _logger.LogInformation("Getting product images");

        IFormFile[]? files;
        try
        {
            files = Request.Form.Files.ToArray();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Could not read form files: {}", ex.Message);
            _logger.LogTrace("{}", ex.StackTrace);
            return Problem("Could not read form files");
        }

        if (files.Length == 0)
        {
            _logger.LogWarning("Product with name {} doesn't have an image", product.Name);
            return BadRequest("Product doesn't have an image");
        }

        _logger.LogInformation("Verifying model");
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model invalid, not uploading product with name {}", product.Name);
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Generating Guid");
        var productId = Guid.NewGuid();

        var apiPaths = await ImageManipulation.WriteImagesToDiskAsync(_staticImagesPathDisk, StaticImagesPathApi,
            _logger, files, productId);

        if (apiPaths is null)
        {
            return Problem("There were problems in creating the files");
        }

        _logger.LogInformation("Creating product with name {}", product.Name);
        var createdProduct = await _catalogItemRepository.CreateCatalogItem(product.AsCatalogItem(apiPaths, productId));

        if (createdProduct == null)
        {
            _logger.LogWarning("Product with name {} could not be created", product.Name);
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Product with name {} created successfully", product.Name);
        return Ok(createdProduct.AsGetDto());
    }

    [HttpPatch("update_item/{id:guid}")]
    public async Task<ActionResult<CatalogItemGetDto>> UpdateCatalogItemDefault(Guid id,
        [FromForm] JsonPatchDocument<CatalogItemCreateDto> patchDoc)
    {
        return await UpdateCatalogItem(patchDoc, id);
    }

    [HttpPatch("update_item/{id:guid}/{boolAddImage:bool}")]
    public async Task<ActionResult<CatalogItemGetDto>> UpdateCatalogItem(
        [FromForm] JsonPatchDocument<CatalogItemCreateDto> patchDoc, Guid id, bool boolAddImage = true)
    {
        _logger.LogInformation("PatchDoc is not null");

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("PatchDoc is not valid");
            return BadRequest(ModelState);
        }

        _logger.LogInformation("PatchDoc is valid");

        var oldItem = await _catalogItemRepository.GetCatalogItemById(id);

        if (oldItem is null)
        {
            _logger.LogWarning("Item with id {} doesn't exist", id);
            return NotFound();
        }

        _logger.LogInformation("Item with id {} found", id);

        IFormFile[]? files;
        var apiPaths = oldItem.ImageLocation;
        try
        {
            files = Request.Form.Files.ToArray();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Could not read form files: {}", ex.Message);
            _logger.LogTrace("{}", ex.StackTrace);
            return Problem("Could not read form files");
        }

        (apiPaths, var problem) = await HandleImageUpdateCatalogItem(id, boolAddImage, files, oldItem, apiPaths);
        if (problem.Result is not OkResult) return problem;

        var oldItemCreateDto = oldItem.AsCreateDto();

        patchDoc.ApplyTo(oldItemCreateDto);

        var newItem = oldItemCreateDto.AsCatalogItem(apiPaths, oldItem.Id, oldItem.CreatedDate);

        _logger.LogInformation("Replacing item with id {}", id);
        var returnedItem = await _catalogItemRepository.ReplaceCatalogItemById(newItem, id);
        _logger.LogInformation("Replacement with item {} was successfully applied", id);

        return Ok(returnedItem);
    }

    [HttpDelete("delete_item/{id:guid}")]
    public async Task<ActionResult<CatalogItemGetDto>> DeleteCatalogItem(Guid id)
    {
        _logger.LogInformation("Deleting item with id {}", id);
        var item = await _catalogItemRepository.DeleteCatalogItemById(id);

        if (item is null)
        {
            _logger.LogWarning("Item with id {} was not found", id);
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Item with id {} was successfully deleted and archived", id);
        return Ok(item);
    }
}