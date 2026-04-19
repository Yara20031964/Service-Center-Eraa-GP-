using System;
using System.Threading.Tasks;
using Domain.Common;
using KHDMA.Application.DTOs.Admin;

namespace KHDMA.Application.Interfaces.Services.Admin
{
    public interface IAdminReviewService
    {
        Task<PagedResponse<ReviewDto>> GetAllReviewsAsync(int pageNumber, int pageSize, string? providerId, string? customerId, int? minRating, int? maxRating);
        Task<ApiResponse<ReviewDto>> GetReviewDetailsAsync(Guid reviewId);
        Task<ApiResponse<bool>> HideOrDeleteReviewAsync(Guid reviewId, bool isDeleted, bool isHidden);
    }
}
