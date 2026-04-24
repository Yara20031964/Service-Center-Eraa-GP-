using Application.DTOs.Admin;
using Application.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
 [Authorize(Roles = "Admin")]
public class AdminProvidersController : ControllerBase
{
    private readonly IAdminProviderService _service;

    public AdminProvidersController(IAdminProviderService service)
    {
        _service = service;
    }

    // GET api/admin/users/providers/pending?page=1&pageSize=10
    [HttpGet("providers/pending")]
    public async Task<IActionResult> GetPending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetPendingApplicationsAsync(page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/users/providers/5/approve-reject
    [HttpPut("providers/{id}/approve-reject")]
    public async Task<IActionResult> ApproveOrReject(string id, [FromBody] ApproveRejectDto dto)
    {
        var result = await _service.ApproveOrRejectApplicationAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    // GET api/admin/users/providers?search=ahmed&page=1&pageSize=10
    [HttpGet("providers")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllProvidersAsync(search, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    // GET api/admin/users/providers/5
    [HttpGet("providers/{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetProviderByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/users/providers/5/suspend
    [HttpPut("providers/{id}/suspend")]
    public async Task<IActionResult> Suspend(string id)
    {
        var result = await _service.SuspendProviderAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/users/providers/5/ban
    [HttpPut("providers/{id}/ban")]
    public async Task<IActionResult> Ban(string id)
    {
        var result = await _service.BanProviderAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/users/providers/5/restore
    [HttpPut("providers/{id}/restore")]
    public async Task<IActionResult> Restore(string id)
    {
        var result = await _service.RestoreProviderAsync(id);
        return StatusCode(result.StatusCode, result);
    }
}