using Microsoft.AspNetCore.Mvc;
using Orders.Bll;

namespace Orders.Api.Controllers;

[ApiController,Route("api/customers")]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct)=>Ok(await service.GetAllAsync(ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<CustomerDto>> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(id,ct));
    [HttpPost] public async Task<ActionResult<CustomerDto>> Create([FromBody]CustomerWriteDto dto,CancellationToken ct){var created=await service.CreateAsync(dto,ct);return CreatedAtAction(nameof(Get),new{id=created.CustomerId},created);}
    [HttpPut("{id:guid}")] public async Task<ActionResult<CustomerDto>> Update(Guid id,[FromBody]CustomerWriteDto dto,CancellationToken ct)=>Ok(await service.UpdateAsync(id,dto,ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id,CancellationToken ct){await service.DeleteAsync(id,ct);return NoContent();}
}
