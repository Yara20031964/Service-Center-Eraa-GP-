using Application.DTOs.Admin;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminContentController : ControllerBase
{
    private readonly IAdminContentService _service;

    public AdminContentController(IAdminContentService service)
    {
        _service = service;
    }

    // ── BANNERS ───────────────────────────────────────────────
    // GET api/admin/banners
    [HttpGet("banners")]
    public async Task<IActionResult> GetBanners()
    {
        var result = await _service.GetAllBannersAsync();
        return StatusCode(result.StatusCode, result);
    }

    // POST api/admin/banners
    [HttpPost("banners")]
    public async Task<IActionResult> CreateBanner([FromBody] CreateBannerDto dto)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var result = await _service.CreateBannerAsync(dto, adminId);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/banners/{id}/toggle
    [HttpPut("banners/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleBanner(Guid id)
    {
        var result = await _service.ToggleBannerAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // DELETE api/admin/banners/{id}
    [HttpDelete("banners/{id:guid}")]
    public async Task<IActionResult> DeleteBanner(Guid id)
    {
        var result = await _service.DeleteBannerAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // ── CANCELLATION POLICY ───────────────────────────────────
    // GET api/admin/cancellation-policy
    [HttpGet("cancellation-policy")]
    public async Task<IActionResult> GetPolicy()
    {
        var result = await _service.GetCancellationPolicyAsync();
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/cancellation-policy
    [HttpPut("cancellation-policy")]
    public async Task<IActionResult> UpdatePolicy([FromBody] UpdateCancellationPolicyDto dto)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var result = await _service.UpdateCancellationPolicyAsync(dto, adminId);
        return StatusCode(result.StatusCode, result);
    }

    // ── PAYOUTS ───────────────────────────────────────────────
    // GET api/admin/payouts
    [HttpGet("payouts")]
    public async Task<IActionResult> GetPayouts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllPayoutsAsync(page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/payouts/{id}/approve
    [HttpPut("payouts/{id:guid}/approve")]
    public async Task<IActionResult> ApprovePayout(Guid id)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var result = await _service.ApprovePayoutAsync(id, adminId);
        return StatusCode(result.StatusCode, result);
    }
}