using Domain.Common;
using KHDMA.Application.DTOs.Auth.Request;
using KHDMA.Application.DTOs.Auth.Response;

namespace KHDMA.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto);
        Task<AuthResponseDto> RegisterProviderAsync(RegisterProviderDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> AdminLoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<ApiResponse<string>> SendEmailConfirmationAsync(string userId);
        Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token);
    }
}
