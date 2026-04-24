using Application.DTOs.Payment;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/payments")]
 [Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymobService _paymobService;

    public PaymentController(IPaymobService paymobService)
    {
        _paymobService = paymobService;
    }

    // POST api/payments/initiate
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] PaymentInitDto dto)
    {
        var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "temp-id";
        var result = await _paymobService.InitiatePaymentAsync(dto, customerId);
        return StatusCode(result.StatusCode, result);
    }

    // POST api/payments/webhook
    // Paymob calls this after payment
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromBody] PaymentWebhookDto dto,
        [FromQuery] string hmac)
    {
        var result = await _paymobService.HandleWebhookAsync(dto, hmac);
        return StatusCode(result.StatusCode, result);
    }

    // POST api/payments/{id}/refund
    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id)
    {
        var result = await _paymobService.RefundAsync(id);
        return StatusCode(result.StatusCode, result);
    }
}