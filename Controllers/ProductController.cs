using System.Text.Json;
using keyset.Data;
using keyset.Services;
using Microsoft.AspNetCore.Mvc;

namespace keyset.Controllers;

[Route("[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductServices _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IProductServices productServices, ILogger<ProductController> logger)
    {
        _logger = logger;
        _productService = productServices;
    }

    [HttpGet("AllAtOnce")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductDTO>))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductDTO>>> GetAllProducts()
    {
        _logger.LogInformation("Retrieving all products");

        try
        {
            var output = await _productService.GetAllProductsAsync();

            if (!output.Any())
            {
                return NoContent();
            }

            return Ok(output);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving all products");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductDTO>))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductDTO>))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, NoStore = false)]

    public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProducts(int pageSize, int? lastPruductId = null)
    {
        if (pageSize < 0)
        {
            return BadRequest("PagaSize must be greated than zero");
        }

        var pagedResult = await _productService.GetPageProductAsync(pageSize, lastPruductId);

        var previousPageUrl = pagedResult.HasPreviousPage
            ? Url.Action("GetProducts", new { pageSize, lastPruductId = pagedResult.Items.First().Id })
            : null;

        var nextPageUrl = pagedResult.HasNextPage
            ? Url.Action("GetProducts", new { pageSize, lastPruductId = pagedResult.Items.Last().Id })
            : null;


        //  What Are We Trying to Do ? 
        // We're building an API that returns paginated data. Instead of returning all 1000 records at once.
        // But the client needs to know: 
        //  * How many total pages exist?
        //  * What's the current page?
        //  * How many total records?
        //
        //
        // How to Send This "Metadata"? 

        // {
        //   "pagination": {
        //     "currentPage": 1,
        //     "totalPages": 50,
        //     "pageSize": 20,
        //     "totalCount": 1000
        //   },
        //   "data": [
        //     { "id": 1, "name": "Item 1" },
        //     { "id": 2, "name": "Item 2" }
        //     // ... 18 more items
        //   ]
        // }
        // Problem: Client has to dig through nested structure to get actual data.

        // Option 2: Put metadata in HTTP headers (Clean separation)
        // Response Body (clean data):
        //  [
        //   { "id": 1, "name": "Item 1" },
        //   { "id": 2, "name": "Item 2" }
        //  ]

        // Response Header (metadata):
        // X-Pagination: {"currentPage":1,"totalPages":50,"pageSize":20,"totalCount":1000}

        var paginationMetadata = new
        {
            PageSize = pagedResult.PageSize,
            HasPreviousPage = pagedResult.HasPreviousPage,
            HasNextPage = pagedResult.HasNextPage,
            PreviousPageUrl = previousPageUrl,
            AveragePricePerCategory = pagedResult.AveragePricePerCategory,
            NextPageUrl = nextPageUrl,
            FirstPageUrl = Url.Action("GetProducts", new { pageSize }),
            LastPageUrl = Url.Action("GetProducts", new { pageSize, lastProductId = (pagedResult.TotalPages - 1) * pageSize })
        };


        // why using this ?
        // Problem: Default JSON serialization ESCAPES characters
        // string json = JsonSerializer.Serialize(paginationMetadata);
        // Result: "{\"CurrentPage\":1,\"TotalPages\":50,\"PageSize\":20,\"TotalCount\":1000}"

        //The Solution: Relaxed JSON Escaping
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(paginationMetadata, options));

        return Ok(pagedResult.Items);
    }
}


