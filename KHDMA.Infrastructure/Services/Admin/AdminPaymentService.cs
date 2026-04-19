using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Infrastructure.Data;

namespace KHDMA.Infrastructure.Services.Admin
{
    public class AdminPaymentService : IAdminPaymentService
    {
        private readonly AppDbContext _context;
        private readonly IStripePaymentService _stripeService;

        public AdminPaymentService(AppDbContext context, IStripePaymentService stripeService)
        {
            _context = context;
            _stripeService = stripeService;
        }

        public async Task<PagedResponse<PaymentDto>> GetAllPaymentsAsync(int pageNumber, int pageSize, PaymentStatus? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Payments
                .Include(p => p.Booking.Customer.ApplicationUser)
                .Include(p => p.Booking.Provider.ApplicationUser)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(p => p.PaymentStatus == status.Value);

            if (fromDate.HasValue)
                query = query.Where(p => p.PaidAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(p => p.PaidAt <= toDate.Value);

            int totalCount = await query.CountAsync();

            var payments = await query
                .OrderByDescending(p => p.PaidAt ?? DateTime.MaxValue)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    BookingId = p.BookingId,
                    Amount = p.Amount,
                    CommissionAmount = p.CommissionAmount,
                    ProviderEarning = p.ProviderEarning,
                    PaymentStatus = p.PaymentStatus,
                    TransactionReference = p.TransactionReference,
                    PaidAt = p.PaidAt,
                    CustomerName = p.Booking.Customer.ApplicationUser.FullName,
                    ProviderName = p.Booking.Provider.ApplicationUser.FullName
                })
                .ToListAsync();

            return PagedResponse<PaymentDto>.Ok(payments, totalCount, pageNumber, pageSize);
        }

        public async Task<ApiResponse<PaymentDto>> GetPaymentDetailsAsync(Guid paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking.Customer.ApplicationUser)
                .Include(p => p.Booking.Provider.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return ApiResponse<PaymentDto>.Fail("Payment not found");

            var dto = new PaymentDto
            {
                Id = payment.Id,
                BookingId = payment.BookingId,
                Amount = payment.Amount,
                CommissionAmount = payment.CommissionAmount,
                ProviderEarning = payment.ProviderEarning,
                PaymentStatus = payment.PaymentStatus,
                TransactionReference = payment.TransactionReference,
                PaidAt = payment.PaidAt,
                CustomerName = payment.Booking.Customer.ApplicationUser.FullName,
                ProviderName = payment.Booking.Provider.ApplicationUser.FullName
            };

            return ApiResponse<PaymentDto>.Ok(dto);
        }

        public async Task<ApiResponse<bool>> IssueRefundAsync(RefundDto refundDto)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == refundDto.PaymentId);
                
            if (payment == null) return ApiResponse<bool>.Fail("Payment not found");

            if (!string.IsNullOrEmpty(payment.TransactionReference))
            {
                var stripeResult = await _stripeService.RefundPaymentAsync(payment.TransactionReference, refundDto.RefundAmount, refundDto.Reason);
                if (!stripeResult.Success) return ApiResponse<bool>.Fail(stripeResult.Message);
            }

            payment.PaymentStatus = PaymentStatus.Refunded;
            if (payment.Booking != null)
            {
                payment.Booking.Status = BookingStatus.Cancelled;
                payment.Booking.Notes = string.IsNullOrEmpty(payment.Booking.Notes) ? $"Refund issued: {refundDto.Reason}" : $"{payment.Booking.Notes}\nRefund issued: {refundDto.Reason}";
            }
            
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Refund processed successfully");
        }

        public async Task<ApiResponse<object>> GetProviderEarningsSummaryAsync(string providerId)
        {
            providerId = providerId.Trim('{', '}', ' ');

            var allPaymentsForProvider = await _context.Payments
                .Include(p => p.Booking)
                .Where(p => p.Booking.ProviderId == providerId)
                .ToListAsync();

            var paidPaymentsForProvider = allPaymentsForProvider
                .Where(p => p.PaymentStatus == PaymentStatus.Paid)
                .ToList();

            var summary = new
            {
                TotalEarned = paidPaymentsForProvider.Sum(p => p.ProviderEarning),
                TotalCommissionDeducted = paidPaymentsForProvider.Sum(p => p.CommissionAmount),
                TotalBookings = paidPaymentsForProvider.Count
            };

            return ApiResponse<object>.Ok(summary);
        }

        public async Task<PagedResponse<PaymentDto>> GetProviderEarningsBreakdownAsync(string providerId, int pageNumber, int pageSize)
        {
            providerId = providerId.Trim('{', '}', ' ');

            var query = _context.Payments
                .Include(p => p.Booking.Customer.ApplicationUser)
                .Include(p => p.Booking.Provider.ApplicationUser)
                .Where(p => p.Booking.ProviderId == providerId && p.PaymentStatus == PaymentStatus.Paid)
                .OrderByDescending(p => p.PaidAt);

            int totalCount = await query.CountAsync();

            var breakdown = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    BookingId = p.BookingId,
                    Amount = p.Amount,
                    CommissionAmount = p.CommissionAmount,
                    ProviderEarning = p.ProviderEarning,
                    PaymentStatus = p.PaymentStatus,
                    PaidAt = p.PaidAt,
                    CustomerName = p.Booking.Customer.ApplicationUser.FullName,
                    ProviderName = p.Booking.Provider.ApplicationUser.FullName 
                })
                .ToListAsync();

            return PagedResponse<PaymentDto>.Ok(breakdown, totalCount, pageNumber, pageSize);
        }
    }
}
