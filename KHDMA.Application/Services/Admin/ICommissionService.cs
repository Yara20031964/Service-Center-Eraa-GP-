using Application.DTOs.Admin;
using Domain.Common;

namespace Application.Services.Admin;

public interface ICommissionService
{
    Task<ApiResponse<CommissionDto>> GetCurrentRateAsync();
    Task<ApiResponse<CommissionDto>> UpdateRateAsync(UpdateCommissionDto dto, string updatedByAdminId);
}