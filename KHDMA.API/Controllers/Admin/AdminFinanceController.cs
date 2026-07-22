using KHDMA.Application.Interfaces.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminFinanceController : ControllerBase
{
    private readonly IAdminFinanceService _service;

    public AdminFinanceController(IAdminFinanceService service)
    {
        _service = service;
    }

    // GET api/admin/transactions?status=Paid&dateFrom=2024-01-01&page=1&pageSize=10
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllTransactionsAsync(
            status, dateFrom, dateTo, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    // GET api/admin/reports/revenue?period=monthly&dateFrom=2024-01-01
    [HttpGet("reports/revenue")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] string period = "monthly",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var result = await _service.GetRevenueReportAsync(period, dateFrom, dateTo);
        return StatusCode(result.StatusCode, result);
    }
}