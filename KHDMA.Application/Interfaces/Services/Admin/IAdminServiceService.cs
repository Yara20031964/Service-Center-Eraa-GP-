using Domain.Common;
using KHDMA.Application.DTOs.Catalog;
using Microsoft.AspNetCore.Http;

namespace KHDMA.Application.Interfaces.Services.Admin;

public interface IAdminServiceService
{
    Task<PagedResponse<ServiceDto>> GetAllAsync(string? search, Guid? categoryId, bool? isActive, int page, int pageSize);
    Task<ApiResponse<ServiceDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<ServiceDto>> CreateAsync(CreateServiceDto dto);
    Task<ApiResponse<ServiceDto>> UpdateAsync(Guid id, UpdateServiceDto dto);
    Task<ApiResponse<string>> ToggleActiveAsync(Guid id);
    Task<ApiResponse<List<string>>> GetImagesAsync(Guid serviceId);
    Task<ApiResponse<List<string>>> AddImagesAsync(Guid serviceId, List<IFormFile> images);
    Task<ApiResponse<string>> DeleteImageAsync(Guid imageId);
}
