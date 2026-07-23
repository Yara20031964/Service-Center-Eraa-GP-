using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Application.DTOs.Catalog;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Application.Interfaces.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers
{
    /// <summary>
    /// Catalogue browsing without a token. SRS 10.1 lets a visitor see what is on
    /// offer before creating an account.
    /// </summary>
    [ApiController]
    [Route("api")]
    [AllowAnonymous]
    [Tags(ApiTags.PublicCatalog)]
    public class PublicCatalogController : ControllerBase
    {
        private readonly IPublicCatalogService _catalog;

        public PublicCatalogController(IPublicCatalogService catalog) => _catalog = catalog;

        [HttpGet("categories/public")]
        [ProducesResponseType<ApiResponse<List<PublicCategoryDto>>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _catalog.GetCategoriesAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("services/public")]
        [ProducesResponseType<PagedResponse<PublicServiceDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetServices(
            [FromQuery] Guid? categoryId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _catalog.GetServicesAsync(categoryId, search, page, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("services/public/{id:guid}")]
        [ProducesResponseType<ApiResponse<PublicServiceDetailDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<PublicServiceDetailDto>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetService(Guid id)
        {
            var result = await _catalog.GetServiceByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("providers/public")]
        [ProducesResponseType<PagedResponse<PublicProviderCardDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProviders(
            [FromQuery] Guid? category,
            [FromQuery] string? search,
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] double? radiusKm,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _catalog.GetProvidersAsync(
                category, search, lat, lng, radiusKm, page, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("providers/{id}/public")]
        [ProducesResponseType<ApiResponse<ProviderPublicProfileDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<ProviderPublicProfileDto>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProviderProfile(string id)
        {
            var result = await _catalog.GetProviderProfileAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }

    /// <summary>Operational overview for the admin console.</summary>
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    [Tags(ApiTags.AdminDashboard)]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _dashboard;

        public AdminDashboardController(IAdminDashboardService dashboard) => _dashboard = dashboard;

        [HttpGet("summary")]
        [ProducesResponseType<ApiResponse<DashboardSummaryDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _dashboard.GetSummaryAsync();
            return StatusCode(result.StatusCode, result);
        }
    }
}
