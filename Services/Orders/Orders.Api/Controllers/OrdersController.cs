using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orders.Bll;
using Platform.ServiceDefaults;

namespace Orders.Api.Controllers;

[ApiController, Route("api/orders")]
public sealed class OrdersController(
    IOrderService service,
    ILogger<OrdersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderDto dto,
        CancellationToken ct)
    {
        using var activity = BusinessTelemetry.Source.StartActivity("Order.Create");

        var created = await service.CreateAsync(dto, ct);

        activity?.SetTag("OrderId", created.OrderId);
        activity?.SetTag("Status", created.Status);

        logger.LogInformation(
            "Order created. {OrderId} {Status}",
            created.OrderId,
            created.Status);

        return CreatedAtAction(nameof(Get), new { id = created.OrderId }, created);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> ChangeStatus(
        Guid id,
        [FromBody] UpdateOrderStatusDto dto,
        CancellationToken ct)
    {
        using var activity = BusinessTelemetry.Source.StartActivity("Order.ChangeStatus");

        var updated = await service.ChangeStatusAsync(id, dto, ct);

        activity?.SetTag("OrderId", updated.OrderId);
        activity?.SetTag("Status", updated.Status);

        logger.LogInformation(
            "Order status changed. {OrderId} {Status}",
            updated.OrderId,
            updated.Status);

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        using var activity = BusinessTelemetry.Source.StartActivity("Order.Delete");

        await service.DeleteAsync(id, ct);

        activity?.SetTag("OrderId", id);
        activity?.SetTag("Status", "Deleted");

        logger.LogInformation(
            "Order deleted. {OrderId} {Status}",
            id,
            "Deleted");

        return NoContent();
    }
}