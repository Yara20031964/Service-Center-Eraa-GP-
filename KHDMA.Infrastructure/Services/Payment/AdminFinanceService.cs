using Application.DTOs.Payments;
using Domain.Common;
using KHDMA.Application.Interfaces.Payment;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using PaymentEntity = KHDMA.Domain.Entities.Payment;
namespace KHDMA.Infrastructure.Services.Admin;

public class AdminFinanceService : IAdminFinanceService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminFinanceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── GET ALL TRANSACTIONS ──────────────────────────────────
    public async Task<PagedResponse<TransactionDto>> GetAllTransactionsAsync(
        string? status, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize)
    {
        var all = await _unitOfWork.Repository<PaymentEntity>()
            .GetAsync(tracked: false);

        // Filter by status
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            all = all.Where(p => p.PaymentStatus == parsedStatus);

        // Filter by date
        if (dateFrom.HasValue)
            all = all.Where(p => p.PaidAt >= dateFrom.Value);

        if (dateTo.HasValue)
            all = all.Where(p => p.PaidAt <= dateTo.Value);

        var totalCount = all.Count();

        var items = all
            .OrderByDescending(p => p.PaidAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new TransactionDto
            {
                Id = p.Id,
                BookingId = p.BookingId,
                Amount = p.Amount,
                CommissionAmount = p.CommissionAmount,
                ProviderEarning = p.ProviderEarning,
                PaymentStatus = p.PaymentStatus.ToString(),
                TransactionReference = p.TransactionReference,
                PaidAt = p.PaidAt
            });

        return PagedResponse<TransactionDto>.Ok(items, totalCount, page, pageSize);
    }

    // ── REVENUE REPORT ────────────────────────────────────────
    public async Task<ApiResponse<RevenueReportDto>> GetRevenueReportAsync(
        string period, DateTime? dateFrom, DateTime? dateTo)
    {
        var all = await _unitOfWork.Repository<PaymentEntity>()
            .GetAsync(p => p.PaymentStatus == PaymentStatus.Paid, tracked: false);

        // Date range filter
        if (dateFrom.HasValue)
            all = all.Where(p => p.PaidAt >= dateFrom.Value);
        if (dateTo.HasValue)
            all = all.Where(p => p.PaidAt <= dateTo.Value);

        // Build breakdown based on period
        List<RevenueByPeriodDto> breakdown = period.ToLower() switch
        {
            "daily" => all
                .GroupBy(p => p.PaidAt!.Value.Date)
                .Select(g => new RevenueByPeriodDto
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    Revenue = g.Sum(p => p.Amount),
                    Commission = g.Sum(p => p.CommissionAmount),
                    Transactions = g.Count()
                })
                .OrderBy(x => x.Period)
                .ToList(),

            "weekly" => all
                .GroupBy(p => $"{p.PaidAt!.Value.Year}-W{GetWeekNumber(p.PaidAt.Value)}")
                .Select(g => new RevenueByPeriodDto
                {
                    Period = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    Commission = g.Sum(p => p.CommissionAmount),
                    Transactions = g.Count()
                })
                .OrderBy(x => x.Period)
                .ToList(),

            "monthly" => all
                .GroupBy(p => p.PaidAt!.Value.ToString("yyyy-MM"))
                .Select(g => new RevenueByPeriodDto
                {
                    Period = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    Commission = g.Sum(p => p.CommissionAmount),
                    Transactions = g.Count()
                })
                .OrderBy(x => x.Period)
                .ToList(),

            _ => new List<RevenueByPeriodDto>()
        };

        var report = new RevenueReportDto
        {
            TotalRevenue = all.Sum(p => p.Amount),
            TotalCommission = all.Sum(p => p.CommissionAmount),
            TotalProviderEarnings = all.Sum(p => p.ProviderEarning),
            TotalTransactions = all.Count(),
            Breakdown = breakdown
        };

        return ApiResponse<RevenueReportDto>.Ok(report);
    }

    private static int GetWeekNumber(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(
            date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
    }
}