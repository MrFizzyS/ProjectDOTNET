using Microsoft.AspNetCore.Mvc;
using Orders.Bll;

namespace Orders.Api.Controllers;

[ApiController,Route("api/orders")]
public sealed class OrdersController(IOrderService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken ct)=>Ok(await service.GetAllAsync(ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<OrderDto>> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(id,ct));
    [HttpPost] public async Task<ActionResult<OrderDto>> Create([FromBody]CreateOrderDto dto,CancellationToken ct){var created=await service.CreateAsync(dto,ct);return CreatedAtAction(nameof(Get),new{id=created.OrderId},created);}
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<OrderDto>> ChangeStatus(Guid id,[FromBody]UpdateOrderStatusDto dto,CancellationToken ct)=>Ok(await service.ChangeStatusAsync(id,dto,ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id,CancellationToken ct){await service.DeleteAsync(id,ct);return NoContent();}
}
