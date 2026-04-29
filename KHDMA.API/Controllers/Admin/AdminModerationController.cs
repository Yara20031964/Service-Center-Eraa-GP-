using Application.DTOs.Admin;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminModerationController : ControllerBase
{
    private readonly IAdminModerationService _service;

    public AdminModerationController(IAdminModerationService service)
    {
        _service = service;
    }

    // ── NOTIFICATION TEMPLATES ────────────────────────────────
    // GET api/admin/notification-templates
    [HttpGet("notification-templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var result = await _service.GetAllTemplatesAsync();
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/notification-templates/{id}
    [HttpPut("notification-templates/{id:guid}")]
    public async Task<IActionResult> UpdateTemplate(
        Guid id, [FromBody] UpdateNotificationTemplateDto dto)
    {
        var result = await _service.UpdateTemplateAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    // ── REVIEW MODERATION ─────────────────────────────────────
    // PUT api/admin/reviews/{id}/hide
    [HttpPut("reviews/{id:guid}/hide")]
    public async Task<IActionResult> HideReview(
        Guid id, [FromBody] HideReviewDto dto)
    {
        var result = await _service.HideReviewAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    // DELETE api/admin/reviews/{id}
    [HttpDelete("reviews/{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var result = await _service.DeleteReviewAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // ── IMPERSONATE ───────────────────────────────────────────
    // POST api/admin/users/{id}/impersonate
    [HttpPost("users/{id}/impersonate")]
    public async Task<IActionResult> Impersonate(string id)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var result = await _service.ImpersonateAsync(id, adminId);
        return StatusCode(result.StatusCode, result);
    }

    // ── BULK PROVIDER ACTIONS ─────────────────────────────────
    // POST api/admin/providers/bulk-approve
    [HttpPost("providers/bulk-approve")]
    public async Task<IActionResult> BulkApprove([FromBody] BulkProviderActionDto dto)
    {
        var result = await _service.BulkApproveProvidersAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    // POST api/admin/providers/bulk-reject
    [HttpPost("providers/bulk-reject")]
    public async Task<IActionResult> BulkReject([FromBody] BulkProviderActionDto dto)
    {
        var result = await _service.BulkRejectProvidersAsync(dto);
        return StatusCode(result.StatusCode, result);
    }
}