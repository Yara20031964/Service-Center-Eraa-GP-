using Application.DTOs.Payment;
using Domain.Common;
using KHDMA.Application.Interfaces.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/payments")]
 [Authorize]
[Tags(ApiTags.CustomerPayments)]
public class PaymentController : ControllerBase
{
    private readonly IPaymobService _paymobService;

    public PaymentController(IPaymobService paymobService)
    {
        _paymobService = paymobService;
    }

    // POST api/payments/initiate
    [HttpPost("initiate")]
    [ProducesResponseType<ApiResponse<PaymentKeyResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<PaymentKeyResponseDto>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<PaymentKeyResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Initiate([FromBody] PaymentInitDto dto)
    {
        var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "temp-id";
        var result = await _paymobService.InitiatePaymentAsync(dto, customerId);
        return StatusCode(result.StatusCode, result);
    }

    // POST api/payments/webhook
    // Paymob calls this after payment. The gateway has no JWT, so the controller's
    // [Authorize] has to be lifted here or every callback comes back 401 and the
    // payment is never confirmed. Authentication is the HMAC signature instead -
    // HandleWebhookAsync verifies it before trusting the body.
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Webhook(
        [FromBody] PaymentWebhookDto dto,
        [FromQuery] string hmac)
    {
        var result = await _paymobService.HandleWebhookAsync(dto, hmac);
        return StatusCode(result.StatusCode, result);
    }

    // POST api/payments/{id}/refund
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Refund(Guid id)
    {
        var result = await _paymobService.RefundAsync(id);
        return StatusCode(result.StatusCode, result);
    }
}
