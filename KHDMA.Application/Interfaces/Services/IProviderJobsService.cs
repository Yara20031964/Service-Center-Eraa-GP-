using Domain.Common;
using KHDMA.Application.DTOs.Provider;

namespace KHDMA.Application.Interfaces.Services;

public interface IProviderJobsService
{
    Task<ApiResponse<List<PendingJobDto>>> GetPendingJobsAsync(string providerId);
    Task<ApiResponse<ProviderAvailabilityDto>> UpdateAvailabilityAsync(
        string providerId, UpdateAvailabilityDto dto);

    /// <summary>The active catalogue, flagged with what this provider offers.</summary>
    Task<ApiResponse<List<ProviderServiceDto>>> GetServicesAsync(string providerId);

    Task<ApiResponse<List<ProviderServiceDto>>> UpdateServicesAsync(
        string providerId, UpdateProviderServicesDto dto);
}
