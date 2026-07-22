using Domain.Common;
using KHDMA.Application.DTOs.Admin;

namespace KHDMA.Application.Interfaces.Services.Admin
{
    public interface IAdminDashboardService
    {
        Task<ApiResponse<DashboardSummaryDto>> GetSummaryAsync();
    }
}
