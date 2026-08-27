using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Supplies.DTOs;
using pcms.Application.Supplies.Interfaces;
namespace pcms.Api.Controllers;
[ApiController, Route("api/[controller]")]
public class SupplyItemsController(ISupplyItemService service) : ControllerBase
{
 [HttpPost, Authorize(Policy="Inventory.Manage")] public async Task<ActionResult<SupplyItemDto>> Create(SupplyItemInput input){try{var item=await service.CreateAsync(input);return CreatedAtAction(nameof(GetById),new{id=item.Id},item);}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return BadRequest(new{message=e.Message});}}
 [HttpGet, Authorize(Policy="Inventory.View")] public async Task<ActionResult<PagedSupplyItemsDto>> GetAll(int page=1,int pageSize=10,string? search=null,bool isActive=true)=>Ok(await service.GetAllAsync(page,pageSize,search,isActive));
 [HttpGet("scan/{code}"), Authorize(Policy="Inventory.View")] public async Task<ActionResult<SupplyItemDto>> GetByScanCode(string code)=>(await service.GetByScanCodeAsync(code)) is { } item?Ok(item):NotFound();
 [HttpGet("{id:guid}"), Authorize(Policy="Inventory.View")] public async Task<ActionResult<SupplyItemDto>> GetById(Guid id)=>(await service.GetByIdAsync(id)) is { } item?Ok(item):NotFound();
 [HttpPut("{id:guid}"), Authorize(Policy="Inventory.Manage")] public async Task<ActionResult<SupplyItemDto>> Update(Guid id,SupplyItemInput input){try{return (await service.UpdateAsync(id,input)) is { } item?Ok(item):NotFound();}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return BadRequest(new{message=e.Message});}}
 [HttpDelete("{id:guid}"), Authorize(Policy="Inventory.Manage")] public async Task<IActionResult> Deactivate(Guid id)=>await service.DeactivateAsync(id)?NoContent():NotFound();
 [HttpPatch("{id:guid}/restore"), Authorize(Policy="Inventory.Manage")] public async Task<IActionResult> Restore(Guid id)=>await service.RestoreAsync(id)?NoContent():NotFound();
}
