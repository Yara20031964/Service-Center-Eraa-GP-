using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Infrastructure.Data;

namespace KHDMA.Infrastructure.Services.Admin
{
    public class AdminBookingService : IAdminBookingService
    {
        private readonly AppDbContext _context;

        public AdminBookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<BookingListDto>> GetAllBookingsAsync(int pageNumber, int pageSize, BookingStatus? status, DateTime? fromDate, DateTime? toDate, string? customerId, string? providerId)
        {
            var query = _context.Bookings
                .Include(b => b.Customer.ApplicationUser)
                .Include(b => b.Provider.ApplicationUser)
                .Include(b => b.Service)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(b => b.Status == status.Value);

            if (fromDate.HasValue)
                query = query.Where(b => b.CreateAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(b => b.CreateAt <= toDate.Value);

            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(b => b.CustomerId == customerId);

            if (!string.IsNullOrEmpty(providerId))
                query = query.Where(b => b.ProviderId == providerId);

            int totalCount = await query.CountAsync();
            
            var bookings = await query
                .OrderByDescending(b => b.CreateAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingListDto
                {
                    Id = b.Id,
                    CustomerName = b.Customer.ApplicationUser.FullName,
                    ProviderName = b.Provider.ApplicationUser.FullName,
                    ServiceName = b.Service.NameEn,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    ScheduledTime = b.ScheduledTime,
                    CreateAt = b.CreateAt
                })
                .ToListAsync();

            return PagedResponse<BookingListDto>.Ok(bookings, totalCount, pageNumber, pageSize);
        }

        public async Task<ApiResponse<BookingDetailDto>> GetBookingDetailsAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer.ApplicationUser)
                .Include(b => b.Provider.ApplicationUser)
                .Include(b => b.Service)
                .Include(b => b.Payment)
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return ApiResponse<BookingDetailDto>.Fail("Booking not found");

            var dto = new BookingDetailDto
            {
                Id = booking.Id,
                CustomerId = booking.CustomerId,
                CustomerName = booking.Customer.ApplicationUser != null ? booking.Customer.ApplicationUser.FullName : "Unknown",
                ProviderId = booking.ProviderId,
                ProviderName = booking.Provider.ApplicationUser != null ? booking.Provider.ApplicationUser.FullName : "Unknown",
                ServiceId = booking.ServiceId,
                ServiceName = booking.Service.NameEn,
                BookingType = booking.BookingType,
                Status = booking.Status,
                ScheduledTime = booking.ScheduledTime,
                Address = booking.Address,
                TotalPrice = booking.TotalPrice,
                Notes = booking.Notes,
                CancelReason = booking.CancelReason,
                CreateAt = booking.CreateAt,
                
                PaymentDetails = booking.Payment != null ? new PaymentDto
                {
                    Id = booking.Payment.Id,
                    BookingId = booking.Payment.BookingId,
                    Amount = booking.Payment.Amount,
                    CommissionAmount = booking.Payment.CommissionAmount,
                    ProviderEarning = booking.Payment.ProviderEarning,
                    PaymentStatus = booking.Payment.PaymentStatus,
                    TransactionReference = booking.Payment.TransactionReference,
                    PaidAt = booking.Payment.PaidAt,
                    CustomerName = booking.Customer.ApplicationUser?.FullName,
                    ProviderName = booking.Provider.ApplicationUser?.FullName
                } : null,

                ReviewDetails = booking.Review != null ? new ReviewDto
                {
                    Id = booking.Review.Id,
                    BookingId = booking.Review.BookingId,
                    Rating = booking.Review.Rating,
                    Comment = booking.Review.Comment,
                    PunctualityRating = booking.Review.PunctualityRating,
                    WorkQualityRating = booking.Review.WorkQualityRating,
                    CleanlinesRating = booking.Review.CleanlinesRating,
                    CreateAt = booking.Review.CreateAt,
                    IsHidden = booking.Review.IsHidden,
                    CustomerName = booking.Customer.ApplicationUser?.FullName,
                    ProviderName = booking.Provider.ApplicationUser?.FullName
                } : null
            };

            return ApiResponse<BookingDetailDto>.Ok(dto);
        }

        public async Task<ApiResponse<bool>> CancelBookingAsync(Guid bookingId, string reason)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return ApiResponse<bool>.Fail("Booking not found");

            booking.Status = BookingStatus.Cancelled;
            booking.CancelReason = reason;

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Booking cancelled successfully");
        }

        public async Task<ApiResponse<object>> GetBookingStatusHistoryAsync(Guid bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return ApiResponse<object>.Fail("Booking not found");

            var history = new 
            {
                BookingId = booking.Id,
                CurrentStatus = booking.Status.ToString(),
                CreatedAt = booking.CreateAt,
                ScheduledTime = booking.ScheduledTime
                // Future: implement literal status history log table if required!
            };
            return ApiResponse<object>.Ok(history);
        }

        public async Task<PagedResponse<ChatTranscriptDto>> GetChatTranscriptAsync(Guid bookingId, int page, int pageSize)
        {
            var query = _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.BookingId == bookingId)
                .AsQueryable();

            int totalCount = await query.CountAsync();

            var messages = await query
                .OrderBy(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ChatTranscriptDto
                {
                    MessageId = m.Id,
                    SenderName = m.Sender.FullName,
                    MessageText = m.MessageText ?? "",
                    MessageType = m.MessageType,
                    SentAt = m.SentAt,
                    AttachmentUrl = m.AttachmentUrl
                })
                .ToListAsync();

            return PagedResponse<ChatTranscriptDto>.Ok(messages, totalCount, page, pageSize);
        }
    }
}
