using Domain.Common;
using KHDMA.Application.Common;
using KHDMA.Application.DTOs.Booking;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KHDMA.Infrastructure.Services;

public class BookingDetailsService : IBookingDetailsService
{
    private readonly AppDbContext _db;
    private readonly IImageUrlResolver _imageUrlResolver;
    private readonly VatSettings _vat;

    public BookingDetailsService(
        AppDbContext db, IImageUrlResolver imageUrlResolver, IOptions<VatSettings> vat)
    {
        _db = db;
        _imageUrlResolver = imageUrlResolver;
        _vat = vat.Value;
    }

    public async Task<ApiResponse<BookingDetailDto>> GetAsync(Guid bookingId, string userId)
    {
        var booking = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Service)
            .Include(b => b.Customer)
                .ThenInclude(c => c.ApplicationUser)
            .Include(b => b.Provider)
                .ThenInclude(p => p!.ApplicationUser)
            .Include(b => b.Review)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return ApiResponse<BookingDetailDto>.NotFound("Booking not found");

        if (booking.CustomerId != userId && booking.ProviderId != userId)
            return ApiResponse<BookingDetailDto>.Forbidden();

        var isAssignedProvider = booking.ProviderId == userId;

        // Mapping stays in memory because unassigned bookings have no Provider,
        // and expression trees cannot contain the null-conditional operators.
        var detail = new[] { booking }
            .AsEnumerable()
            .Select(b => new BookingDetailDto
            {
                Id = b.Id,
                CustomerId = b.CustomerId,
                CustomerName = b.Customer?.ApplicationUser?.FullName ?? string.Empty,
                ProviderId = b.Provider?.ApplicationUserId,
                ProviderName = b.Provider?.ApplicationUser?.FullName,
                ProviderPhone = b.Provider?.ApplicationUser?.PhoneNumber,
                ProviderRating = b.Provider?.Rating,
                ProviderPhoto = _imageUrlResolver.Resolve(b.Provider?.ApplicationUser?.ProfilePictureUrl),
                ServiceId = b.ServiceId,
                ServiceName = b.Service?.NameEn ?? string.Empty,
                ServiceNameEn = b.Service?.NameEn ?? string.Empty,
                ServiceNameAr = b.Service?.NameAr ?? string.Empty,
                BookingType = b.BookingType,
                ScheduledTime = b.ScheduledTime,
                Address = b.Address,
                Latitude = b.Latitude,
                Longitude = b.Longitude,
                Status = b.Status,
                StatusLabelEn = BookingStatusLabels.En(b.Status),
                StatusLabelAr = BookingStatusLabels.Ar(b.Status),
                TotalPrice = b.TotalPrice,
                Notes = b.Notes,
                CancelReason = b.CancelReason,
                CreateAt = b.CreateAt,
                AcceptedAt = b.AcceptedAt,
                EnRouteAt = b.EnRouteAt,
                ArrivedAt = b.ArrivedAt,
                StartedAt = b.StartedAt,
                CompletedAt = b.CompletedAt,
                CancelledAt = b.CancelledAt,
                CancellationFee = b.CancellationFee,
                CustomerPhone = isAssignedProvider
                    ? b.Customer?.ApplicationUser?.PhoneNumber
                    : null,
                ProviderEarning = isAssignedProvider ? b.Payment?.ProviderEarning : null,
                Currency = isAssignedProvider ? _vat.Currency : null,
                // Hidden reviews stay visible to the two participants - hiding is
                // admin moderation for the public provider listing, and the author
                // still needs to see and edit their own review.
                Review = b.Review is null || b.Review.IsDeleted
                    ? null
                    : new BookingReviewDto
                    {
                        Id = b.Review.Id,
                        Rating = b.Review.Rating,
                        Comment = b.Review.Comment,
                        ProviderReply = b.Review.ProviderReply,
                        ProviderReplyAt = b.Review.ProviderReplyAt,
                        PunctualityRating = b.Review.PunctualityRating,
                        WorkQualityRating = b.Review.WorkQualityRating,
                        CleanlinessRating = b.Review.CleanlinesRating,
                        CreateAt = b.Review.CreateAt,
                    },
            })
            .Single();

        return ApiResponse<BookingDetailDto>.Ok(detail);
    }
}
