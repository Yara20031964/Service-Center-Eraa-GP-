using Application.DTOs.Admin;
using Application.Interfaces.Services;
using Domain.Common;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using PaymentEntity = KHDMA.Domain.Entities.Payment;
namespace KHDMA.Infrastructure.Services.Admin;

public class AdminContentService : IAdminContentService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminContentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<IEnumerable<BannerDto>>> GetAllBannersAsync()
    {
        var banners = await _unitOfWork.Repository<Banner>()
            .GetAsync(tracked: false);

        var items = banners
            .OrderBy(b => b.Order)
            .Select(b => new BannerDto
            {
                Id = b.Id,
                Title = b.Title,
                ImageUrl = b.ImageUrl,
                IsActive = b.IsActive,
                Order = b.Order,
                CreatedAt = b.CreatedAt
            });

        return ApiResponse<IEnumerable<BannerDto>>.Ok(items);
    }

    public async Task<ApiResponse<BannerDto>> CreateBannerAsync(
        CreateBannerDto dto, string adminId)
    {
        var banner = new Banner
        {
            Title = dto.Title,
            ImageUrl = dto.ImageUrl,
            Order = dto.Order,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Banner>().CreateAsync(banner);
        await _unitOfWork.CommitAsync();

        return ApiResponse<BannerDto>.Created(new BannerDto
        {
            Id = banner.Id,
            Title = banner.Title,
            ImageUrl = banner.ImageUrl,
            IsActive = banner.IsActive,
            Order = banner.Order,
            CreatedAt = banner.CreatedAt
        });
    }

    public async Task<ApiResponse<string>> ToggleBannerAsync(Guid id)
    {
        var banner = await _unitOfWork.Repository<Banner>()
            .GetOneAsync(b => b.Id == id);

        if (banner is null)
            return ApiResponse<string>.NotFound("Banner not found");

        banner.IsActive = !banner.IsActive;
        _unitOfWork.Repository<Banner>().Update(banner);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok(
            banner.IsActive ? "Banner activated" : "Banner deactivated");
    }

    public async Task<ApiResponse<string>> DeleteBannerAsync(Guid id)
    {
        var banner = await _unitOfWork.Repository<Banner>()
            .GetOneAsync(b => b.Id == id);

        if (banner is null)
            return ApiResponse<string>.NotFound("Banner not found");

        _unitOfWork.Repository<Banner>().Delete(banner);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Banner deleted successfully");
    }

    // ── CANCELLATION POLICY ───────────────────────────────────
    public async Task<ApiResponse<CancellationPolicyDto>> GetCancellationPolicyAsync()
    {
        var policy = await _unitOfWork.Repository<CancellationPolicy>()
            .GetOneAsync(c => c.Id == 1);

        if (policy is null)
            return ApiResponse<CancellationPolicyDto>.NotFound("Policy not found");

        return ApiResponse<CancellationPolicyDto>.Ok(new CancellationPolicyDto
        {
            FreeCancelWindowMinutes = policy.FreeCancelWindowMinutes,
            CancellationFee = policy.CancellationFee,
            LastUpdatedAt = policy.LastUpdatedAt,
            UpdatedBy = policy.UpdatedBy
        });
    }

    public async Task<ApiResponse<CancellationPolicyDto>> UpdateCancellationPolicyAsync(
        UpdateCancellationPolicyDto dto, string adminId)
    {
        var policy = await _unitOfWork.Repository<CancellationPolicy>()
            .GetOneAsync(c => c.Id == 1);

        if (policy is null)
            return ApiResponse<CancellationPolicyDto>.NotFound("Policy not found");

        policy.FreeCancelWindowMinutes = dto.FreeCancelWindowMinutes;
        policy.CancellationFee = dto.CancellationFee;
        policy.LastUpdatedAt = DateTime.UtcNow;
        policy.UpdatedBy = adminId;

        _unitOfWork.Repository<CancellationPolicy>().Update(policy);
        await _unitOfWork.CommitAsync();

        return ApiResponse<CancellationPolicyDto>.Ok(new CancellationPolicyDto
        {
            FreeCancelWindowMinutes = policy.FreeCancelWindowMinutes,
            CancellationFee = policy.CancellationFee,
            LastUpdatedAt = policy.LastUpdatedAt,
            UpdatedBy = policy.UpdatedBy
        });
    }

    // ── PAYOUTS ───────────────────────────────────────────────
    public async Task<PagedResponse<PayoutDto>> GetAllPayoutsAsync(
        int page, int pageSize)
    {
        var payments = await _unitOfWork.Repository<PaymentEntity>()
            .GetAsync(
                p => p.PaymentStatus == KHDMA.Domain.Enums.PaymentStatus.Paid,
                includes: [p => p.Booking],
                tracked: false);

        var totalCount = payments.Count();

        var items = payments
            .OrderByDescending(p => p.PaidAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PayoutDto
            {
                Id = p.Id,
                ProviderId = p.Booking.ProviderId,
                Amount = p.ProviderEarning,
                Status = "Pending",
                CreatedAt = p.PaidAt ?? DateTime.UtcNow
            });

        return PagedResponse<PayoutDto>.Ok(items, totalCount, page, pageSize);
    }

    public async Task<ApiResponse<string>> ApprovePayoutAsync(Guid id, string adminId)
    {
        var payment = await _unitOfWork.Repository<PaymentEntity>()
            .GetOneAsync(p => p.Id == id &&
                              p.PaymentStatus == KHDMA.Domain.Enums.PaymentStatus.Paid);

        if (payment is null)
            return ApiResponse<string>.NotFound("Payout not found");

        // Mark as processed — في المستقبل هنضيف Payout entity منفصلة
        return ApiResponse<string>.Ok("Payout approved successfully");
    }
}