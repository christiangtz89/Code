using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using pcms.Application.Reporting;using pcms.Domain.Enums;
namespace pcms.Api.Controllers;
[ApiController,Route("api/reports/inventory"),Authorize(Policy="Inventory.View")]
public class InventoryReportsController(IInventoryReportService reports):ControllerBase { [HttpGet] public async Task<ActionResult<InventoryReportDto>> Get(DateTime? startDate=null,DateTime? endDate=null,string? search=null,SupplyInventoryMovementType? movementType=null)=>Ok(await reports.GetAsync(startDate,endDate,search,movementType)); }
