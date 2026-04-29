using Application.DTOs.Admin;
using Domain.Common;

namespace Application.Interfaces.Services;

public interface IAdminContentService
{
    Task<ApiResponse<IEnumerable<BannerDto>>> GetAllBannersAsync();
    Task<ApiResponse<BannerDto>> CreateBannerAsync(CreateBannerDto dto, string adminId);
    Task<ApiResponse<string>> ToggleBannerAsync(Guid id);
    Task<ApiResponse<string>> DeleteBannerAsync(Guid id);

    Task<ApiResponse<CancellationPolicyDto>> GetCancellationPolicyAsync();
    Task<ApiResponse<CancellationPolicyDto>> UpdateCancellationPolicyAsync(
        UpdateCancellationPolicyDto dto, string adminId);

    Task<PagedResponse<PayoutDto>> GetAllPayoutsAsync(int page, int pageSize);
    Task<ApiResponse<string>> ApprovePayoutAsync(Guid id, string adminId);
}