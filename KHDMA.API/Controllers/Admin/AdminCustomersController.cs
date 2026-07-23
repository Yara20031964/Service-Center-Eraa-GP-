using KHDMA.Application.Interfaces.Services.Admin;
using Application.DTOs.Admin;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
 [Authorize(Roles = "Admin")]
[Tags(ApiTags.AdminCustomers)]
public class AdminCustomersController : ControllerBase
{
    private readonly IAdminCustomerService _service;

    public AdminCustomersController(IAdminCustomerService service)
    {
        _service = service;
    }

    // GET api/admin/users/customers?search=ahmed&page=1&pageSize=10
    [HttpGet("customers")]
    [ProducesResponseType<PagedResponse<CustomerDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllCustomersAsync(search, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    // GET api/admin/users/customers/5
    [HttpGet("customers/{id}")]
    [ProducesResponseType<ApiResponse<CustomerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<CustomerDto>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetCustomerByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/users/customers/5/suspend
    [HttpPut("customers/{id}/suspend")]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suspend(string id)
    {
        var result = await _service.SuspendCustomerAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/users/customers/5/ban
    [HttpPut("customers/{id}/ban")]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ban(string id)
    {
        var result = await _service.BanCustomerAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/users/customers/5/restore
    [HttpPut("customers/{id}/restore")]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(string id)
    {
        var result = await _service.RestoreCustomerAsync(id);
        return StatusCode(result.StatusCode, result);
    }
}
