using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using System.Security.Claims; using pcms.Application.Inventory;
namespace pcms.Api.Controllers;
[ApiController,Route("api/urn-productions")]
public class ManufacturedUrnProductionsController(IManufacturedUrnProductionService service):ControllerBase
{
 [HttpGet,Authorize(Policy="Inventory.View")] public async Task<ActionResult<IEnumerable<ProductionDto>>> Get([FromQuery] Guid? urnId)=>Ok(await service.GetAllAsync(urnId,false));
 [HttpGet("costs"),Authorize(Policy="Purchasing.View")] public async Task<ActionResult<IEnumerable<ProductionDto>>> Costs([FromQuery] Guid? urnId)=>Ok(await service.GetAllAsync(urnId,true));
 [HttpPost,Authorize(Policy="Inventory.Manage")] public async Task<ActionResult<ProductionDto>> Create(ProductionInput input){try{var uid=Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:(Guid?)null;return Ok(await service.CreateAsync(input,uid));}catch(ArgumentException e){return BadRequest(new{message=e.Message});}catch(InvalidOperationException e){return BadRequest(new{message=e.Message});}}
}
