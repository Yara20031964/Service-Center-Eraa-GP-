using Domain.Common;
using KHDMA.Application.DTOs.Profile;
using Microsoft.AspNetCore.Http;

namespace KHDMA.Application.Interfaces.Services;

public interface IProfileService
{
    Task<ApiResponse<object>> GetProfileAsync(string userId);
    Task<ApiResponse<object>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    Task<ApiResponse<List<AddressDto>>> GetAddressesAsync(string userId);
    Task<ApiResponse<AddressDto>> AddAddressAsync(string userId, CreateAddressDto dto);
    Task<ApiResponse<string>> DeleteAddressAsync(string userId, Guid addressId);
    Task<ApiResponse<List<string>>> GetCertificateImagesAsync(string userId);
    Task<ApiResponse<List<string>>> AddCertificateImagesAsync(string userId, List<IFormFile> images);
    Task<ApiResponse<string>> DeleteCertificateImageAsync(string userId, Guid imageId);
    Task<ApiResponse<List<string>>> GetPortfolioImagesAsync(string userId);
    Task<ApiResponse<List<string>>> AddPortfolioImagesAsync(string userId, List<IFormFile> images);
    Task<ApiResponse<string>> DeletePortfolioImageAsync(string userId, Guid imageId);
}
