using Application.DTOs.Admin;
using Domain.Common;

namespace Application.Interfaces.Services;

public interface IAdminModerationService
{
    // Notification Templates
    Task<ApiResponse<IEnumerable<NotificationTemplateDto>>> GetAllTemplatesAsync();
    Task<ApiResponse<NotificationTemplateDto>> UpdateTemplateAsync(
        Guid id, UpdateNotificationTemplateDto dto);

    // Review Moderation
    Task<ApiResponse<string>> HideReviewAsync(Guid id, HideReviewDto dto);
    Task<ApiResponse<string>> DeleteReviewAsync(Guid id);

    // Impersonate
    Task<ApiResponse<string>> ImpersonateAsync(string targetUserId, string adminId);

    // Bulk Provider Actions
    Task<ApiResponse<string>> BulkApproveProvidersAsync(BulkProviderActionDto dto);
    Task<ApiResponse<string>> BulkRejectProvidersAsync(BulkProviderActionDto dto);
}