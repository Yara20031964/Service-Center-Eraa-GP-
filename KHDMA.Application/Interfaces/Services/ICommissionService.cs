using Application.DTOs.Admin;
using Domain.Common;

namespace KHDMA.Application.Interfaces.Services;

public interface ICommissionService
{
    Task<ApiResponse<CommissionDto>> GetCurrentRateAsync();
    Task<ApiResponse<CommissionDto>> UpdateRateAsync(UpdateCommissionDto dto, string updatedByAdminId);
}