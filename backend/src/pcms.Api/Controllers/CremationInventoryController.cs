using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Inventory;
namespace pcms.Api.Controllers;
[ApiController,Route("api/cremations/{cremationId:guid}/inventory"),Authorize]
public sealed class CremationInventoryController(ICremationInventoryService service):ControllerBase
{
 [HttpGet,Authorize(Policy="Inventory.View")] public async Task<ActionResult<CremationInventoryDto>> Get(Guid cremationId)=>Ok(await service.GetAsync(cremationId));
 [HttpPost("reserve"),Authorize(Policy="Inventory.Manage")] public async Task<ActionResult<CremationInventoryDto>> Reserve(Guid cremationId)=>Ok(await service.ReserveAsync(cremationId,UserId()));
 [HttpPost("cancel"),Authorize(Policy="Inventory.Manage")] public async Task<ActionResult<CremationInventoryDto>> Cancel(Guid cremationId,[FromBody] string? reason=null)=>Ok(await service.CancelAsync(cremationId,UserId(),reason));
 [HttpPost("fulfill"),Authorize(Policy="Inventory.Manage")] public async Task<ActionResult<CremationInventoryDto>> Fulfill(Guid cremationId,IReadOnlyCollection<FulfillmentMaterialInput> materials)=>Ok(await service.FulfillAsync(cremationId,materials,UserId()));
 Guid? UserId()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
}
