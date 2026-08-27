using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Supplies.DTOs;
using pcms.Application.Supplies.Interfaces;
namespace pcms.Api.Controllers;
[ApiController, Route("api/[controller]")]
public class SuppliersController(ISupplierService service) : ControllerBase
{
 [HttpPost, Authorize(Policy="Suppliers.Manage")] public async Task<ActionResult<SupplierDto>> Create(SupplierInput input){try{var item=await service.CreateAsync(input);return CreatedAtAction(nameof(GetById),new{id=item.Id},item);}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return BadRequest(new{message=e.Message});}}
 [HttpGet, Authorize(Policy="Suppliers.View")] public async Task<ActionResult<PagedSuppliersDto>> GetAll(int page=1,int pageSize=10,string? search=null,bool isActive=true)=>Ok(await service.GetAllAsync(page,pageSize,search,isActive));
 [HttpGet("{id:guid}"), Authorize(Policy="Suppliers.View")] public async Task<ActionResult<SupplierDto>> GetById(Guid id)=>(await service.GetByIdAsync(id)) is { } item?Ok(item):NotFound();
 [HttpPut("{id:guid}"), Authorize(Policy="Suppliers.Manage")] public async Task<ActionResult<SupplierDto>> Update(Guid id,SupplierInput input){try{return (await service.UpdateAsync(id,input)) is { } item?Ok(item):NotFound();}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return BadRequest(new{message=e.Message});}}
 [HttpDelete("{id:guid}"), Authorize(Policy="Suppliers.Manage")] public async Task<IActionResult> Deactivate(Guid id)=>await service.DeactivateAsync(id)?NoContent():NotFound();
 [HttpPatch("{id:guid}/restore"), Authorize(Policy="Suppliers.Manage")] public async Task<IActionResult> Restore(Guid id)=>await service.RestoreAsync(id)?NoContent():NotFound();
}
