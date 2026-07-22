using Application.DTOs.Admin;
using KHDMA.Application.Interfaces.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
// [Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _service;

    public AdminUsersController(IAdminUserService service)
    {
        _service = service;
    }

    // GET api/admin/users/admins?search=&page=1&pageSize=10
    [HttpGet("admins")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAdminsAsync(search, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    // GET api/admin/users/admins/{id}
    [HttpGet("admins/{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetAdminByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // POST api/admin/users/admins
    [HttpPost("admins")]
    public async Task<IActionResult> Create([FromBody] CreateAdminDto dto)
    {
        var result = await _service.CreateAdminAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/users/admins/{id}/deactivate
    [HttpPut("admins/{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var result = await _service.DeactivateAdminAsync(id);
        return StatusCode(result.StatusCode, result);
    }
}