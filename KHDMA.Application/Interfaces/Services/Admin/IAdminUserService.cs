using Application.DTOs.Admin;
using Domain.Common;

namespace KHDMA.Application.Interfaces.Services.Admin;

public interface IAdminUserService
{
    Task<PagedResponse<AdminUserDto>> GetAllAdminsAsync(
        string? search, int page, int pageSize);

    Task<ApiResponse<AdminUserDto>> GetAdminByIdAsync(string id);
    Task<ApiResponse<AdminUserDto>> CreateAdminAsync(CreateAdminDto dto);
    Task<ApiResponse<string>> DeactivateAdminAsync(string id);
}