using Domain.Common;
using KHDMA.Application.DTOs.Catalog;

namespace KHDMA.Application.Interfaces.Services
{
    /// <summary>
    /// Anonymous browsing. SRS 10.1 lets a visitor see the catalogue before
    /// signing up, so none of these require a token.
    /// </summary>
    public interface IPublicCatalogService
    {
        Task<ApiResponse<List<PublicCategoryDto>>> GetCategoriesAsync();

        Task<PagedResponse<PublicServiceDto>> GetServicesAsync(
            Guid? categoryId, string? search, int page, int pageSize);

        Task<ApiResponse<PublicServiceDetailDto>> GetServiceByIdAsync(Guid id);

        Task<ApiResponse<ProviderPublicProfileDto>> GetProviderProfileAsync(string providerId);
    }
}
