using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KHDMA.Domain.Entities;
using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Infrastructure.Data;

namespace KHDMA.Infrastructure.Services.Admin
{
    public class AdminReviewService : IAdminReviewService
    {
        private readonly AppDbContext _context;

        public AdminReviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<ReviewDto>> GetAllReviewsAsync(int pageNumber, int pageSize, string? providerId, string? customerId, int? minRating, int? maxRating)
        {
            var query = _context.Reviews
                .Include(r => r.Customer.ApplicationUser)
                .Include(r => r.Provider.ApplicationUser)
                .Where(r => !r.IsDeleted) // Default to not showing deleted
                .AsQueryable();

            if (!string.IsNullOrEmpty(providerId))
                query = query.Where(r => r.ProviderId == providerId);

            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(r => r.CustomerId == customerId);

            if (minRating.HasValue)
                query = query.Where(r => r.Rating >= minRating.Value);

            if (maxRating.HasValue)
                query = query.Where(r => r.Rating <= maxRating.Value);

            int totalCount = await query.CountAsync();

            var reviews = await query
                .OrderByDescending(r => r.CreateAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    BookingId = r.BookingId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    PunctualityRating = r.PunctualityRating,
                    WorkQualityRating = r.WorkQualityRating,
                    CleanlinesRating = r.CleanlinesRating,
                    CreateAt = r.CreateAt,
                    IsHidden = r.IsHidden,
                    CustomerName = r.Customer.ApplicationUser.FullName,
                    ProviderName = r.Provider.ApplicationUser.FullName
                })
                .ToListAsync();

            return PagedResponse<ReviewDto>.Ok(reviews, totalCount, pageNumber, pageSize);
        }

        public async Task<ApiResponse<ReviewDto>> GetReviewDetailsAsync(Guid reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.Customer.ApplicationUser)
                .Include(r => r.Provider.ApplicationUser)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null) return ApiResponse<ReviewDto>.Fail("Review not found");

            var dto = new ReviewDto
            {
                Id = review.Id,
                BookingId = review.BookingId,
                Rating = review.Rating,
                Comment = review.Comment,
                PunctualityRating = review.PunctualityRating,
                WorkQualityRating = review.WorkQualityRating,
                CleanlinesRating = review.CleanlinesRating,
                CreateAt = review.CreateAt,
                IsHidden = review.IsHidden,
                CustomerName = review.Customer.ApplicationUser.FullName,
                ProviderName = review.Provider.ApplicationUser.FullName
            };

            return ApiResponse<ReviewDto>.Ok(dto);
        }

        public async Task<ApiResponse<bool>> HideOrDeleteReviewAsync(Guid reviewId, bool isDeleted, bool isHidden)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return ApiResponse<bool>.Fail("Review not found");

            review.IsDeleted = isDeleted;
            review.IsHidden = isHidden;

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Review status updated successfully");
        }
    }
}
