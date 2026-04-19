using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stripe;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Infrastructure.Data;
using Domain.Common;
using KHDMA.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace KHDMA.Infrastructure.Services.Payment
{
    public class StripePaymentService : IStripePaymentService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public StripePaymentService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            StripeConfiguration.ApiKey = _config["StripeSettings:SecretKey"];
        }

        public async Task<ApiResponse<string>> CreatePaymentIntentAsync(Guid bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return ApiResponse<string>.Fail("Booking not found");

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(booking.TotalPrice * 100),
                Currency = "usd",
                Metadata = new Dictionary<string, string>
                {
                    { "BookingId", booking.Id.ToString() }
                }
            };

            var service = new PaymentIntentService();
            PaymentIntent intent;
            try
            {
                intent = await service.CreateAsync(options);
            }
            catch (StripeException e)
            {
                return ApiResponse<string>.Fail($"Stripe Error: {e.Message}");
            }

            if (payment == null)
            {
                payment = new KHDMA.Domain.Entities.Payment
                {
                    BookingId = booking.Id,
                    Amount = booking.TotalPrice,
                    PaymentStatus = PaymentStatus.Pending,
                    TransactionReference = intent.Id,
                    CommissionAmount = booking.TotalPrice * 0.1m,
                    ProviderEarning = booking.TotalPrice * 0.9m
                };
                _context.Payments.Add(payment);
            }
            else
            {
                payment.TransactionReference = intent.Id;
            }

            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok(intent.ClientSecret);
        }

        public async Task<ApiResponse<bool>> RefundPaymentAsync(string paymentIntentId, decimal amount, string reason)
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = (long)(amount * 100),
                Reason = RefundReasons.RequestedByCustomer
            };

            var refundService = new Stripe.RefundService();
            try
            {
                await refundService.CreateAsync(options);
                return ApiResponse<bool>.Ok(true, "Refund processed on Stripe successfully");
            }
            catch (StripeException e)
            {
                return ApiResponse<bool>.Fail($"Stripe Error: {e.Message}");
            }
        }

        /// <summary>
        /// يُستدعى بعد أن يُؤكد الـ Frontend أن الدفع نجح.
        /// يتحقق من Stripe مباشرة، ثم يُحدّث حالة الدفع والحجز في قاعدة البيانات.
        /// </summary>
        public async Task<ApiResponse<bool>> ConfirmPaymentAsync(string paymentIntentId)
        {
            try
            {
                // 1. تأكد من Stripe أن الدفع نجح فعلاً (لمنع التلاعب)
                var intentService = new PaymentIntentService();
                var intent = await intentService.GetAsync(paymentIntentId);

                if (intent.Status != "succeeded")
                    return ApiResponse<bool>.Fail($"Payment not confirmed by Stripe. Status: {intent.Status}");

                // 2. ابحث عن سجل الدفع في قاعدة البيانات
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionReference == paymentIntentId);

                if (payment == null)
                    return ApiResponse<bool>.Fail("Payment record not found.");

                // 3. حدّث حالة الدفع
                payment.PaymentStatus = PaymentStatus.Paid;
                payment.PaidAt = DateTime.UtcNow;

                // 4. حدّث حالة الحجز إلى Accepted
                var booking = await _context.Bookings.FindAsync(payment.BookingId);
                if (booking != null)
                    booking.Status = BookingStatus.Accepted;

                await _context.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Payment confirmed and booking updated successfully!");
            }
            catch (StripeException e)
            {
                return ApiResponse<bool>.Fail($"Stripe Error: {e.Message}");
            }
        }
    }
}
