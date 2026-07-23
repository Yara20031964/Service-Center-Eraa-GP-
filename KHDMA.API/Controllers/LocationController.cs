using System.Security.Claims;
using Domain.Common;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers
{
    [ApiController]
    [Route("api/location")]
    [Authorize]
    [Tags(ApiTags.ProviderLocation)]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _location;

        public LocationController(ILocationService location) => _location = location;

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// Publish the calling provider's current position. Streamed to any booking
        /// they are actively travelling to; cached for 5 minutes and never persisted
        /// as a trail (SRS 7.2).
        /// </summary>
        /// <remarks>
        /// Exposed as both PUT and POST because the task list and the API contract
        /// each named a different verb; PUT is the canonical one.
        /// </remarks>
        [HttpPut("update")]
        [HttpPost("update")]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update([FromBody] UpdateLocationDto dto)
        {
            if (UserId is null) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var result = await _location.UpdateProviderLocationAsync(UserId, dto);
            return StatusCode(result.StatusCode, result);
        }
    }

    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    [Tags(ApiTags.CustomerBookings)]
    public class BookingEtaController : ControllerBase
    {
        private readonly ILocationService _location;

        public BookingEtaController(ILocationService location) => _location = location;

        /// <summary>
        /// Arrival estimate for a booking, from the provider's live position.
        /// Falls back to a straight-line estimate when Maps is unavailable - check
        /// <c>source</c> before presenting it as precise.
        /// </summary>
        [HttpGet("{id:guid}/eta")]
        [ProducesResponseType<ApiResponse<EtaDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<EtaDto>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<EtaDto>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<EtaDto>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<EtaDto>>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GetEta(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized(ApiResponse<EtaDto>.Unauthorized());

            var result = await _location.GetEtaAsync(id, userId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
