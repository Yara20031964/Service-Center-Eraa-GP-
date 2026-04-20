using KHDMA.Application.DTOs.Auth.Request;
using KHDMA.Application.DTOs.Auth.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto);
        Task<AuthResponseDto> RegisterProviderAsync(RegisterProviderDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task <bool> RevokeTokenAsync(string refreshToken);
    }
}
