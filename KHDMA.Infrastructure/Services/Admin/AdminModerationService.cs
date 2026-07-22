using Application.DTOs.Admin;
using Application.Interfaces.Services;
using Domain.Common;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace KHDMA.Infrastructure.Services.Admin;

public class AdminModerationService : IAdminModerationService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminModerationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── NOTIFICATION TEMPLATES ────────────────────────────────
    public async Task<ApiResponse<IEnumerable<NotificationTemplateDto>>> GetAllTemplatesAsync()
    {
        var templates = await _unitOfWork.Repository<NotificationTemplate>()
            .GetAsync(tracked: false);

        var items = templates.Select(t => new NotificationTemplateDto
        {
            Id = t.Id,
            EventType = t.EventType,
            TitleEn = t.TitleEn,
            TitleAr = t.TitleAr,
            BodyEn = t.BodyEn,
            BodyAr = t.BodyAr,
            IsActive = t.IsActive,
            UpdatedAt = t.UpdatedAt
        });

        return ApiResponse<IEnumerable<NotificationTemplateDto>>.Ok(items);
    }

    public async Task<ApiResponse<NotificationTemplateDto>> UpdateTemplateAsync(
        Guid id, UpdateNotificationTemplateDto dto)
    {
        var template = await _unitOfWork.Repository<NotificationTemplate>()
            .GetOneAsync(t => t.Id == id);

        if (template is null)
            return ApiResponse<NotificationTemplateDto>.NotFound("Template not found");

        template.TitleEn = dto.TitleEn;
        template.TitleAr = dto.TitleAr;
        template.BodyEn = dto.BodyEn;
        template.BodyAr = dto.BodyAr;
        template.IsActive = dto.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<NotificationTemplate>().Update(template);
        await _unitOfWork.CommitAsync();

        return ApiResponse<NotificationTemplateDto>.Ok(new NotificationTemplateDto
        {
            Id = template.Id,
            EventType = template.EventType,
            TitleEn = template.TitleEn,
            TitleAr = template.TitleAr,
            BodyEn = template.BodyEn,
            BodyAr = template.BodyAr,
            IsActive = template.IsActive,
            UpdatedAt = template.UpdatedAt
        });
    }

    // ── REVIEW MODERATION ─────────────────────────────────────
    public async Task<ApiResponse<string>> HideReviewAsync(Guid id, HideReviewDto dto)
    {
        var review = await _unitOfWork.Repository<Review>()
            .GetOneAsync(r => r.Id == id && !r.IsDeleted);

        if (review is null)
            return ApiResponse<string>.NotFound("Review not found");

        if (review.IsHidden)
            return ApiResponse<string>.Fail("Review is already hidden");

        review.IsHidden = true;
        _unitOfWork.Repository<Review>().Update(review);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Review hidden successfully");
    }

    public async Task<ApiResponse<string>> DeleteReviewAsync(Guid id)
    {
        var review = await _unitOfWork.Repository<Review>()
            .GetOneAsync(r => r.Id == id && !r.IsDeleted);

        if (review is null)
            return ApiResponse<string>.NotFound("Review not found");

        review.IsDeleted = true;
        _unitOfWork.Repository<Review>().Update(review);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Review deleted successfully");
    }

    // ── IMPERSONATE ───────────────────────────────────────────
    public async Task<ApiResponse<string>> ImpersonateAsync(
        string targetUserId, string adminId)
    {
        var user = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(u => u.Id == targetUserId && !u.IsDeleted);

        if (user is null)
            return ApiResponse<string>.NotFound("User not found");

        // Log the impersonation in AuditLog
        var log = new AuditLog
        {
            UserId = adminId,
            Action = "IMPERSONATE",
            Target = $"/users/{targetUserId}",
            Timestamp = DateTime.UtcNow,
            StatusCode = 200
        };

        await _unitOfWork.Repository<AuditLog>().CreateAsync(log);
        await _unitOfWork.CommitAsync();

        // هنرجع الـ userId بس — Yara هتعمل الـ token generation
        return ApiResponse<string>.Ok(targetUserId);
    }

    // ── BULK PROVIDER ACTIONS ─────────────────────────────────
    public async Task<ApiResponse<string>> BulkApproveProvidersAsync(
        BulkProviderActionDto dto)
    {
        var providers = await _unitOfWork.Repository<ApplicationUser>()
            .GetAsync(u => dto.ProviderIds.Contains(u.Id) &&
                          u.Role == UserRole.Provider &&
                          u.Provider!.State == ProviderState.Pending &&
                          !u.IsDeleted,
                     includes: [u => u.Provider!]);

        if (!providers.Any())
            return ApiResponse<string>.NotFound("No pending providers found");

        foreach (var provider in providers)
        {
            provider.Status = UserStatus.Active;
            provider.Provider!.State = ProviderState.Active;
            _unitOfWork.Repository<ApplicationUser>().Update(provider);
        }

        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok(
            $"{providers.Count()} providers approved successfully");
    }

    public async Task<ApiResponse<string>> BulkRejectProvidersAsync(
        BulkProviderActionDto dto)
    {
        var providers = await _unitOfWork.Repository<ApplicationUser>()
            .GetAsync(u => dto.ProviderIds.Contains(u.Id) &&
                          u.Role == UserRole.Provider &&
                          u.Provider!.State == ProviderState.Pending &&
                          !u.IsDeleted,
                     includes: [u => u.Provider!]);

        if (!providers.Any())
            return ApiResponse<string>.NotFound("No pending providers found");

        foreach (var provider in providers)
        {
            provider.Status = UserStatus.Banned;
            provider.Provider!.State = ProviderState.Banned;
            _unitOfWork.Repository<ApplicationUser>().Update(provider);
        }

        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok(
            $"{providers.Count()} providers rejected successfully");
    }
}