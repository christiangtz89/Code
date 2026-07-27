using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Customers;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(
        ICustomerService customerService)
    {
        _customerService = customerService;
    }


    [HttpGet]
public async Task<IActionResult> GetAll(
    int page = 1,
    int pageSize = 10)
{
    var customers =
        await _customerService.GetAllAsync(
            page,
            pageSize);

    return Ok(customers);
}


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer =
            await _customerService.GetByIdAsync(id);


        if (customer == null)
            return NotFound();


        return Ok(customer);
    }


    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCustomerDto dto)
    {
        var customer =
            await _customerService.CreateAsync(dto);


        return CreatedAtAction(
            nameof(GetById),
            new { id = customer.Id },
            customer);
    }

    [HttpPut("{id}")]
public async Task<IActionResult> Update(
    Guid id,
    UpdateCustomerDto dto)
{
    var customer =
        await _customerService.UpdateAsync(id, dto);


    if (customer == null)
        return NotFound();


    return Ok(customer);
}

[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id)
{
    var result =
        await _customerService.DeactivateAsync(id);


    if (!result)
        return NotFound();


    return NoContent();
}

[HttpGet("search")]
public async Task<IActionResult> Search(
    [FromQuery] string term)
{
    var customers =
        await _customerService.SearchAsync(term);

    return Ok(customers);
}

[HttpPut("{id}/restore")]
public async Task<IActionResult> Restore(Guid id)
{
    var result =
        await _customerService.RestoreAsync(id);


    if (!result)
        return NotFound();


    return NoContent();
}

}