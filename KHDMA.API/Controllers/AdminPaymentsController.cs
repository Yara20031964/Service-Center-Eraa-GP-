using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace KHDMA.API.Controllers
{
    [Route("api/admin/payments")]
    [ApiController]
    // [Authorize(Roles = "Admin")]
    public class AdminPaymentsController : ControllerBase
    {
        private readonly IAdminPaymentService _paymentService;

        public AdminPaymentsController(IAdminPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] PaymentStatus? status = null,
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            var response = await _paymentService.GetAllPaymentsAsync(page, pageSize, status, fromDate, toDate);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentDetails(Guid id)
        {
            var response = await _paymentService.GetPaymentDetailsAsync(id);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }

        [HttpPost("refund")]
        public async Task<IActionResult> IssueRefund([FromBody] RefundDto refundDto)
        {
            var response = await _paymentService.IssueRefundAsync(refundDto);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("provider-earnings/{providerId}")]
        public async Task<IActionResult> GetProviderEarningsSummary(string providerId)
        {
            var response = await _paymentService.GetProviderEarningsSummaryAsync(providerId);
            return Ok(response);
        }

        [HttpGet("provider-earnings/{providerId}/breakdown")]
        public async Task<IActionResult> GetProviderEarningsBreakdown(string providerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var response = await _paymentService.GetProviderEarningsBreakdownAsync(providerId, page, pageSize);
            return Ok(response);
        }
    }
}
