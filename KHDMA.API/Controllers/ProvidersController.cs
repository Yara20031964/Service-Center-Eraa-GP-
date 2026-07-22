using System.Security.Claims;
using Domain.Common;
using KHDMA.Application.DTOs.Provider;
using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers
{
    /// <summary>
    /// The calling provider's own money. Every route takes the provider id from
    /// the JWT and never from a route or query parameter - otherwise any provider
    /// could read another's earnings by editing the url.
    /// </summary>
    [ApiController]
    [Route("api/providers")]
    [Authorize(Roles = "Provider")]
    [Tags(ApiTags.ProviderEarnings)]
    public class ProvidersController : ControllerBase
    {
        private readonly IEarningsService _earnings;

        public ProvidersController(IEarningsService earnings) => _earnings = earnings;

        private string? ProviderId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>Earnings for a period: daily | weekly | monthly | all.</summary>
        [HttpGet("earnings")]
        public async Task<IActionResult> GetEarnings([FromQuery] string period = "weekly")
        {
            if (ProviderId is null) return Unauthorized(ApiResponse<EarningsDto>.Unauthorized());

            var result = await _earnings.GetEarningsAsync(ProviderId, period);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("wallet")]
        public async Task<IActionResult> GetWallet()
        {
            if (ProviderId is null) return Unauthorized(ApiResponse<WalletDto>.Unauthorized());

            var result = await _earnings.GetWalletAsync(ProviderId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Request a withdrawal against the available balance.</summary>
        [HttpPost("payouts")]
        public async Task<IActionResult> RequestPayout([FromBody] RequestPayoutDto dto)
        {
            if (ProviderId is null) return Unauthorized(ApiResponse<ProviderPayoutDto>.Unauthorized());

            var result = await _earnings.RequestPayoutAsync(ProviderId, dto.Amount);
            return StatusCode(result.StatusCode, result);
        }
    }
}
