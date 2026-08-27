using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Supplies.DTOs;
using pcms.Application.Supplies.Interfaces;
namespace pcms.Api.Controllers;
[ApiController, Route("api/[controller]")]
public class ExpensesController(IExpenseService service) : ControllerBase
{
 [HttpGet("categories"), Authorize(Policy="Finance.View")] public async Task<ActionResult<IEnumerable<ExpenseCategoryDto>>> Categories(bool isActive=true)=>Ok(await service.GetCategoriesAsync(isActive));
 [HttpPost("categories"), Authorize(Policy="Finance.Manage")] public async Task<ActionResult<ExpenseCategoryDto>> CreateCategory(ExpenseCategoryInput input){try{return Ok(await service.CreateCategoryAsync(input));}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return BadRequest(new{message=e.Message});}}
 [HttpPost, Authorize(Policy="Finance.Manage")] public async Task<ActionResult<ExpenseDto>> Create(ExpenseInput input){try{Guid? user=Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;return Ok(await service.CreateAsync(input,user));}catch(ArgumentException e){return BadRequest(new{message=e.Message});}}
 [HttpGet, Authorize(Policy="Finance.View")] public async Task<ActionResult<PagedExpensesDto>> GetAll(int page=1,int pageSize=10,DateTime? from=null,DateTime? to=null,Guid? categoryId=null,Guid? supplierId=null,bool isActive=true)=>Ok(await service.GetAllAsync(page,pageSize,from,to,categoryId,supplierId,isActive));
 [HttpDelete("{id:guid}"), Authorize(Policy="Finance.Manage")] public async Task<IActionResult> Void(Guid id)=>await service.VoidAsync(id)?NoContent():NotFound();
}
