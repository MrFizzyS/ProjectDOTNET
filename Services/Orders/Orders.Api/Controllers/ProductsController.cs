using Microsoft.AspNetCore.Mvc;
using Orders.Bll;

namespace Orders.Api.Controllers;

[ApiController,Route("api/products")]
public sealed class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken ct)=>Ok(await service.GetAllAsync(ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ProductDto>> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(id,ct));
    [HttpPost] public async Task<ActionResult<ProductDto>> Create([FromBody]ProductWriteDto dto,CancellationToken ct){var created=await service.CreateAsync(dto,ct);return CreatedAtAction(nameof(Get),new{id=created.ProductId},created);}
    [HttpPut("{id:guid}")] public async Task<ActionResult<ProductDto>> Update(Guid id,[FromBody]ProductWriteDto dto,CancellationToken ct)=>Ok(await service.UpdateAsync(id,dto,ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id,CancellationToken ct){await service.DeleteAsync(id,ct);return NoContent();}
}
