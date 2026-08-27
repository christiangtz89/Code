using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Reporting;

namespace pcms.Api.Controllers;

[ApiController, Route("api/reports/cost-analytics"), Authorize(Policy = "Purchasing.View")]
public sealed class CostAnalyticsController(ICostAnalyticsService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<CostAnalyticsDto>> Get(DateTime? startDate = null, DateTime? endDate = null) => Ok(await service.GetAsync(startDate, endDate));
}
