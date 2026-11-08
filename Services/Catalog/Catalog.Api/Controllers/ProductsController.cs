using Catalog.Bll.Contracts;
using Catalog.Bll.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Platform.ServiceDefaults;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/catalog/products")]
public sealed class ProductsController(
    IProductService productService,
    ILogger<ProductsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> GetAll(
        [FromQuery] ProductQueryDto query,
        CancellationToken cancellationToken)
        => Ok(await productService.GetAllAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await productService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductDto request,
        CancellationToken cancellationToken)
    {
        using var activity =
            BusinessTelemetry.Source.StartActivity("Catalog.Product.Create");

        var created = await productService.CreateAsync(request, cancellationToken);

        activity?.SetTag("ProductId", created.ProductId);
        activity?.SetTag("Status", "Created");

        logger.LogInformation(
            "Catalog product created. {ProductId} {Status}",
            created.ProductId,
            "Created");

        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id,
        UpdateProductDto request,
        CancellationToken cancellationToken)
    {
        using var activity =
            BusinessTelemetry.Source.StartActivity("Catalog.Product.Update");

        var updated = await productService.UpdateAsync(id, request, cancellationToken);

        activity?.SetTag("ProductId", updated.ProductId);
        activity?.SetTag("Status", "Updated");

        logger.LogInformation(
            "Catalog product updated. {ProductId} {Status}",
            updated.ProductId,
            "Updated");

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var activity =
            BusinessTelemetry.Source.StartActivity("Catalog.Product.Delete");

        await productService.DeleteAsync(id, cancellationToken);

        activity?.SetTag("ProductId", id);
        activity?.SetTag("Status", "Deleted");

        logger.LogInformation(
            "Catalog product deleted. {ProductId} {Status}",
            id,
            "Deleted");

        return NoContent();
    }
}