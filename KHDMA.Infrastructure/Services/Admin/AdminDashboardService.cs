using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services.Admin
{
    /// <summary>
    /// The admin overview panel.
    /// </summary>
    /// <remarks>
    /// Every figure is aggregated by the database. The existing
    /// <c>AdminPaymentService.GetProviderEarningsSummaryAsync</c> loads all matching
    /// rows into memory and sums them in C#, which stops working long before this
    /// dashboard would - so that pattern is deliberately not copied here.
    /// </remarks>
    public class AdminDashboardService : IAdminDashboardService
    {
        private static readonly BookingStatus[] ActiveStatuses =
        [
            BookingStatus.Accepted, BookingStatus.EnRoute,
            BookingStatus.Arrived, BookingStatus.InProgress,
        ];

        private readonly AppDbContext _db;

        public AdminDashboardService(AppDbContext db) => _db = db;

        public async Task<ApiResponse<DashboardSummaryDto>> GetSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekStart = today.AddDays(-6);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var sevenDaysAgo = now.AddDays(-7);

            // One grouped query instead of ten COUNT round trips.
            var statusCounts = await _db.Bookings
                .AsNoTracking()
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            int CountOf(BookingStatus status) => statusCounts.TryGetValue(status, out var n) ? n : 0;

            var bookings = new BookingCountsDto
            {
                Total = statusCounts.Values.Sum(),
                Pending = CountOf(BookingStatus.Pending),
                Dispatching = CountOf(BookingStatus.Dispatching),
                Active = ActiveStatuses.Sum(CountOf),
                Completed = CountOf(BookingStatus.Completed),
                Cancelled = CountOf(BookingStatus.Cancelled),
                NoProviderFound = CountOf(BookingStatus.NoProviderFound),
                Today = await _db.Bookings.CountAsync(b => b.CreateAt >= today),
            };

            var paid = _db.Payments.AsNoTracking().Where(p => p.PaymentStatus == PaymentStatus.Paid);

            var revenue = new RevenueDto
            {
                Today = await paid.Where(p => p.PaidAt >= today).SumAsync(p => (decimal?)p.Amount) ?? 0m,
                ThisWeek = await paid.Where(p => p.PaidAt >= weekStart).SumAsync(p => (decimal?)p.Amount) ?? 0m,
                ThisMonth = await paid.Where(p => p.PaidAt >= monthStart).SumAsync(p => (decimal?)p.Amount) ?? 0m,
                AllTime = await paid.SumAsync(p => (decimal?)p.Amount) ?? 0m,
                CommissionCollected = await paid.SumAsync(p => (decimal?)p.CommissionAmount) ?? 0m,
                PendingPayouts = await _db.Payouts
                    .Where(p => p.Status == PayoutStatus.Requested || p.Status == PayoutStatus.Approved)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m,
            };

            // Time from the first Dispatching transition to acceptance. Computed
            // from BookingStatusHistory, which is why every transition writes a row.
            //
            // The subtraction happens in C# rather than via EF.Functions.DateDiffSecond:
            // both the SqlServer and Pomelo MySQL providers are referenced by this
            // project, and each defines that extension, so the call is ambiguous.
            // The projection is still narrow - two timestamps per booking, capped.
            var acceptTimestamps = await _db.Bookings
                .AsNoTracking()
                .Where(b => b.AcceptedAt != null && b.CreateAt >= sevenDaysAgo)
                .Select(b => new
                {
                    DispatchedAt = b.StatusHistory
                        .Where(h => h.ToStatus == BookingStatus.Dispatching)
                        .OrderBy(h => h.ChangedAt)
                        .Select(h => (DateTime?)h.ChangedAt)
                        .FirstOrDefault(),
                    b.AcceptedAt,
                })
                .Where(x => x.DispatchedAt != null)
                .Take(2000)
                .ToListAsync();

            var acceptSeconds = acceptTimestamps
                .Select(x => (x.AcceptedAt!.Value - x.DispatchedAt!.Value).TotalSeconds)
                .Where(s => s >= 0)
                .ToList();

            var dispatchedLast7 = await _db.Bookings
                .CountAsync(b => b.CreateAt >= sevenDaysAgo && b.DispatchRoundCount > 0);

            var failedLast7 = await _db.Bookings
                .CountAsync(b => b.CreateAt >= sevenDaysAgo && b.Status == BookingStatus.NoProviderFound);

            var operations = new OperationsDto
            {
                ProvidersOnline = await _db.Providers.CountAsync(p =>
                    p.AvailabilityStatus == AvailabilityStatus.Online && p.State == ProviderState.Active),
                ProvidersTotal = await _db.Providers.CountAsync(),
                PendingProviderApplications = await _db.Providers.CountAsync(p => p.State == ProviderState.Pending),
                CustomersTotal = await _db.Customers.CountAsync(),
                AverageDispatchToAcceptSeconds = acceptSeconds.Count > 0
                    ? Math.Round(acceptSeconds.Average(), 1)
                    : null,
                NoProviderRateLast7Days = dispatchedLast7 > 0
                    ? Math.Round((double)failedLast7 / dispatchedLast7, 4)
                    : 0,
            };

            return ApiResponse<DashboardSummaryDto>.Ok(new DashboardSummaryDto
            {
                Bookings = bookings,
                Revenue = revenue,
                Operations = operations,
            });
        }
    }
}
