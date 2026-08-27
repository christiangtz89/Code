using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Reporting;

namespace pcms.Api.Controllers;

[ApiController, Route("api/reports/spending"), Authorize]
public class SpendingReportsController(ISpendingReportService reports, IAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SpendingReportDto>> Get(DateTime? startDate = null, DateTime? endDate = null, Guid? expenseCategoryId = null, Guid? supplierId = null)
    {
        var finance = (await authorization.AuthorizeAsync(User, "Finance.View")).Succeeded;
        var purchasing = (await authorization.AuthorizeAsync(User, "Purchasing.View")).Succeeded;
        if (!finance && !purchasing) return Forbid();
        return Ok(await reports.GetAsync(startDate, endDate, expenseCategoryId, supplierId, finance, purchasing));
    }
}
