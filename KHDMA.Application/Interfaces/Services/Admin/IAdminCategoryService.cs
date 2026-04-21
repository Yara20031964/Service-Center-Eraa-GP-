using Domain.Common;
using KHDMA.Application.DTOs.Catalog;

namespace KHDMA.Application.Interfaces.Services.Admin;

public interface IAdminCategoryService
{
    Task<PagedResponse<CategoryDto>> GetAllAsync(string? search, bool? isActive, int page, int pageSize);
    Task<ApiResponse<CategoryDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto);
    Task<ApiResponse<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto);
    Task<ApiResponse<string>> ToggleActiveAsync(Guid id);
}
