using Application.DTOs.Admin;
using Application.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin/commission")]
 [Authorize(Roles = "Admin")]
public class CommissionController : ControllerBase
{
    private readonly ICommissionService _service;

    public CommissionController(ICommissionService service)
    {
        _service = service;
    }

    // GET api/admin/commission
    [HttpGet]
    public async Task<IActionResult> GetCurrent()
    {
        var result = await _service.GetCurrentRateAsync();
        return StatusCode(result.StatusCode, result);
    }

    // PUT api/admin/commission
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCommissionDto dto)
    {
        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? "system";

        var result = await _service.UpdateRateAsync(dto, adminId);
        return StatusCode(result.StatusCode, result);
    }
}