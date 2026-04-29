using Application.DTOs.Admin;
using Domain.Common;

namespace KHDMA.Application.Interfaces.Services.Admin;

public interface IAdminProviderService
{
    Task<PagedResponse<ProviderApplicationDto>> GetPendingApplicationsAsync(
        int page, int pageSize);

    Task<ApiResponse<string>> ApproveOrRejectApplicationAsync(
        string id, ApproveRejectDto dto);

    Task<PagedResponse<ProviderDto>> GetAllProvidersAsync(
        string? search, int page, int pageSize);

    Task<ApiResponse<ProviderDto>> GetProviderByIdAsync(string id);

    Task<ApiResponse<string>> SuspendProviderAsync(string id);
    Task<ApiResponse<string>> BanProviderAsync(string id);
    Task<ApiResponse<string>> RestoreProviderAsync(string id);
}