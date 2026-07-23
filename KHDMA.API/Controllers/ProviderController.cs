using System.Security.Claims;
using Domain.Common;
using KHDMA.Application.DTOs.Provider;
using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers;

[ApiController]
[Route("api/provider")]
[Authorize(Roles = "Provider")]
[Tags(ApiTags.ProviderJobs)]
public class ProviderController : ControllerBase
{
    private readonly IProviderJobsService _providerJobs;

    public ProviderController(IProviderJobsService providerJobs)
        => _providerJobs = providerJobs;

    [HttpGet("pending-jobs")]
    [ProducesResponseType<ApiResponse<List<PendingJobDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<List<PendingJobDto>>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<List<PendingJobDto>>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPendingJobs()
    {
        var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(providerId))
        {
            var unauthorized = ApiResponse<List<PendingJobDto>>.Unauthorized();
            return StatusCode(unauthorized.StatusCode, unauthorized);
        }

        var result = await _providerJobs.GetPendingJobsAsync(providerId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("availability")]
    [ProducesResponseType<ApiResponse<ProviderAvailabilityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<ProviderAvailabilityDto>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<ProviderAvailabilityDto>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<ProviderAvailabilityDto>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAvailability([FromBody] UpdateAvailabilityDto dto)
    {
        var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(providerId))
        {
            var unauthorized = ApiResponse<ProviderAvailabilityDto>.Unauthorized();
            return StatusCode(unauthorized.StatusCode, unauthorized);
        }

        var result = await _providerJobs.UpdateAvailabilityAsync(providerId, dto);
        return StatusCode(result.StatusCode, result);
    }
}
