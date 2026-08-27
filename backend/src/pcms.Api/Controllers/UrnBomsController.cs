using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using pcms.Application.Inventory;
namespace pcms.Api.Controllers;
[ApiController,Route("api/urn-boms")]
public class UrnBomsController(IUrnBomService service):ControllerBase
{
 [HttpGet("{urnId:guid}"),Authorize(Policy="Inventory.View")] public async Task<ActionResult<IEnumerable<BomDto>>> Get(Guid urnId)=>Ok(await service.GetAsync(urnId,false));
 [HttpGet("{urnId:guid}/cost"),Authorize(Policy="Purchasing.View")] public async Task<ActionResult<IEnumerable<BomDto>>> Cost(Guid urnId)=>Ok(await service.GetAsync(urnId,true));
 [HttpPut,Authorize(Policy="Inventory.Manage")] public async Task<ActionResult<BomDto>> Save(BomInput input){try{return Ok(await service.SaveAsync(input));}catch(ArgumentException e){return BadRequest(new{message=e.Message});}}
}
