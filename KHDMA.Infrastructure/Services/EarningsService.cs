using Domain.Common;
using KHDMA.Application.DTOs.Provider;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KHDMA.Infrastructure.Services
{
    public class EarningsService : IEarningsService
    {
        /// <summary>Earned but not yet withdrawable - the job is not finished.</summary>
        private static readonly BookingStatus[] InFlightStatuses =
        [
            BookingStatus.Accepted, BookingStatus.EnRoute,
            BookingStatus.Arrived, BookingStatus.InProgress,
        ];

        private readonly AppDbContext _db;
        private readonly IPricingService _pricing;
        private readonly ILogger<EarningsService> _logger;

        public EarningsService(AppDbContext db, IPricingService pricing, ILogger<EarningsService> logger)
        {
            _db = db;
            _pricing = pricing;
            _logger = logger;
        }

        // ------------------------------------------------------------------
        // Credit on completion
        // ------------------------------------------------------------------

        public async Task RecordEarningsAsync(Guid bookingId)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking is null) throw new Exception("Booking not found");
            if (booking.Status != BookingStatus.Completed) return;
            if (booking.ProviderId is null) return;

            var provider = await _db.Providers.FirstOrDefaultAsync(p => p.ApplicationUserId == booking.ProviderId);
            if (provider is null) throw new Exception("Provider not found");

            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);

            // Idempotence guard. Completion can be retried, and without this the
            // same earnings would be added to the wallet on every call.
            if (payment is not null && payment.PaidAt is not null
                && payment.ProviderEarning > 0 && payment.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.LogInformation("Earnings for booking {BookingId} already recorded; skipping", bookingId);
                return;
            }

            // Commission comes off the service fee, never off the VAT-inclusive
            // total - VAT is collected for the tax authority, not platform revenue.
            var serviceFee = payment is not null && payment.ServiceFee > 0
                ? payment.ServiceFee
                : booking.TotalPrice;

            var breakdown = await _pricing.ForServiceFeeAsync(serviceFee);

            provider.TotalEarnings += breakdown.ProviderEarning;
            provider.Balance += breakdown.ProviderEarning;
            provider.NumberOfJobsDone++;

            if (payment is not null)
            {
                payment.CommissionAmount = breakdown.CommissionAmount;
                payment.ProviderEarning = breakdown.ProviderEarning;
                payment.PaymentStatus = PaymentStatus.Paid;
                payment.PaidAt ??= DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Credited {Net} to provider {ProviderId} for booking {BookingId} (gross {Gross}, commission {Commission})",
                breakdown.ProviderEarning, booking.ProviderId, bookingId,
                breakdown.ServiceFee, breakdown.CommissionAmount);
        }

        // ------------------------------------------------------------------
        // Reporting
        // ------------------------------------------------------------------

        public async Task<ApiResponse<EarningsDto>> GetEarningsAsync(string providerId, string period)
        {
            var normalised = (period ?? "weekly").Trim().ToLowerInvariant();

            if (normalised is not ("daily" or "weekly" or "monthly" or "all"))
                return ApiResponse<EarningsDto>.Fail("period must be daily, weekly, monthly or all");

            var now = DateTime.UtcNow;

            var from = normalised switch
            {
                "daily" => now.Date,
                "weekly" => now.Date.AddDays(-6),
                "monthly" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                _ => DateTime.MinValue,
            };

            // Filtered and projected in SQL rather than loading every booking into
            // memory the way AdminPaymentService does.
            var rows = await _db.Bookings
                .AsNoTracking()
                .Where(b => b.ProviderId == providerId
                         && b.Status == BookingStatus.Completed
                         && b.CompletedAt != null
                         && b.CompletedAt >= from)
                .OrderByDescending(b => b.CompletedAt)
                .Select(b => new
                {
                    b.Id,
                    ServiceName = b.Service.NameEn,
                    Date = b.CompletedAt!.Value,
                    Gross = b.Payment != null && b.Payment.ServiceFee > 0 ? b.Payment.ServiceFee : b.TotalPrice,
                    Commission = b.Payment != null ? b.Payment.CommissionAmount : 0m,
                    Net = b.Payment != null ? b.Payment.ProviderEarning : 0m,
                })
                .Take(500)
                .ToListAsync();

            // Probe the current rate so the client never hardcodes 15%.
            var probe = await _pricing.ForServiceFeeAsync(100m);

            return ApiResponse<EarningsDto>.Ok(new EarningsDto
            {
                Period = normalised,
                From = from,
                To = now,
                TotalGross = rows.Sum(r => r.Gross),
                TotalCommissionDeducted = rows.Sum(r => r.Commission),
                TotalEarned = rows.Sum(r => r.Net),
                BookingsCount = rows.Count,
                CommissionRate = probe.CommissionRate,
                Currency = probe.Currency,
                Breakdown = rows.Select(r => new EarningsBreakdownItemDto
                {
                    BookingId = r.Id,
                    ServiceName = r.ServiceName,
                    Date = r.Date,
                    Gross = r.Gross,
                    Commission = r.Commission,
                    Net = r.Net,
                }).ToList(),
            });
        }

        public async Task<ApiResponse<WalletDto>> GetWalletAsync(string providerId)
        {
            var provider = await _db.Providers
                .AsNoTracking()
                .Where(p => p.ApplicationUserId == providerId)
                .Select(p => new { p.Balance, p.TotalEarnings })
                .FirstOrDefaultAsync();

            if (provider is null) return ApiResponse<WalletDto>.NotFound("Provider profile not found");

            var pending = await _db.Bookings
                .AsNoTracking()
                .Where(b => b.ProviderId == providerId && InFlightStatuses.Contains(b.Status))
                .SumAsync(b => (decimal?)(b.Payment != null ? b.Payment.ProviderEarning : 0m)) ?? 0m;

            var withdrawn = await _db.Payouts
                .AsNoTracking()
                .Where(p => p.ProviderId == providerId && p.Status == PayoutStatus.Paid)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var recent = await _db.Payouts
                .AsNoTracking()
                .Where(p => p.ProviderId == providerId)
                .OrderByDescending(p => p.RequestedAt)
                .Take(10)
                .Select(p => new ProviderPayoutDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    Status = p.Status.ToString(),
                    RequestedAt = p.RequestedAt,
                    PaidAt = p.PaidAt,
                    Reference = p.Reference,
                })
                .ToListAsync();

            return ApiResponse<WalletDto>.Ok(new WalletDto
            {
                AvailableBalance = provider.Balance,
                PendingBalance = pending,
                TotalEarned = provider.TotalEarnings,
                TotalWithdrawn = withdrawn,
                NextPayoutDate = NextFriday(DateTime.UtcNow),
                RecentPayouts = recent,
            });
        }

        public async Task<ApiResponse<ProviderPayoutDto>> RequestPayoutAsync(string providerId, decimal amount)
        {
            if (amount <= 0)
                return ApiResponse<ProviderPayoutDto>.Fail("Payout amount must be greater than zero");

            var provider = await _db.Providers.FirstOrDefaultAsync(p => p.ApplicationUserId == providerId);
            if (provider is null) return ApiResponse<ProviderPayoutDto>.NotFound("Provider profile not found");

            var outstanding = await _db.Payouts
                .Where(p => p.ProviderId == providerId
                         && (p.Status == PayoutStatus.Requested || p.Status == PayoutStatus.Approved))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // Balance is only decremented when a payout is actually paid, so any
            // in-flight request must be subtracted here - otherwise the same
            // balance could be queued for withdrawal twice.
            var withdrawable = provider.Balance - outstanding;

            if (amount > withdrawable)
                return ApiResponse<ProviderPayoutDto>.Fail(
                    $"Requested amount exceeds your withdrawable balance of {withdrawable:0.00}", 409);

            var payout = new Payout
            {
                ProviderId = providerId,
                Amount = amount,
                Status = PayoutStatus.Requested,
            };

            _db.Payouts.Add(payout);
            await _db.SaveChangesAsync();

            return ApiResponse<ProviderPayoutDto>.Created(new ProviderPayoutDto
            {
                Id = payout.Id,
                Amount = payout.Amount,
                Status = payout.Status.ToString(),
                RequestedAt = payout.RequestedAt,
            }, "Payout requested");
        }

        /// <summary>Payouts are settled weekly, on Friday.</summary>
        private static DateTime NextFriday(DateTime from)
        {
            var daysAhead = ((int)DayOfWeek.Friday - (int)from.DayOfWeek + 7) % 7;
            if (daysAhead == 0) daysAhead = 7;
            return from.Date.AddDays(daysAhead);
        }
    }
}
