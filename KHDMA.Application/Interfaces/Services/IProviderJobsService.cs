using Domain.Common;
using KHDMA.Application.DTOs.Provider;

namespace KHDMA.Application.Interfaces.Services;

public interface IProviderJobsService
{
    Task<ApiResponse<List<PendingJobDto>>> GetPendingJobsAsync(string providerId);
    Task<ApiResponse<ProviderAvailabilityDto>> UpdateAvailabilityAsync(
        string providerId, UpdateAvailabilityDto dto);
}
