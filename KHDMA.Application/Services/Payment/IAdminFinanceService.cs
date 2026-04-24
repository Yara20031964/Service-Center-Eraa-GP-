using Application.DTOs.Payments;
using Domain.Common;

namespace Application.Interfaces.Services;

public interface IAdminFinanceService
{
    Task<PagedResponse<TransactionDto>> GetAllTransactionsAsync(
        string? status, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize);

    Task<ApiResponse<RevenueReportDto>> GetRevenueReportAsync(
        string period, DateTime? dateFrom, DateTime? dateTo);
}