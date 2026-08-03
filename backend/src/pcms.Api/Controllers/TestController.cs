using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{

    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult Authenticated()
    {
        return Ok(new
        {
            message = "Any authenticated user can access"
        });
    }


    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return Ok(new
        {
            message = "Admin access granted"
        });
    }


    [HttpGet("finance")]
    [Authorize(Roles = "Finanzas")]
    public IActionResult Finance()
    {
        return Ok(new
        {
            message = "Finance access granted"
        });
    }


    [HttpGet("sales")]
    [Authorize(Roles = "Ventas")]
    public IActionResult Sales()
    {
        return Ok(new
        {
            message = "Sales access granted"
        });
    }
}