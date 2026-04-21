using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KHDMA.Application.DTOs.Auth.Request;
using KHDMA.Application.DTOs.Auth.Response;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Domain.Common;

namespace KHDMA.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return AuthResponseDto.Fail("Email already registered");

            string? profilePicUrl = null;
            if (dto.ProfilePicture != null)
                profilePicUrl = await SaveFileAsync(dto.ProfilePicture, "profiles");

            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = dto.DateOfBirth,
                ProfilePictureUrl = profilePicUrl,
                Role = UserRole.Customer,
                Status = UserStatus.Active
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return AuthResponseDto.Fail(result.Errors.First().Description);

            await _userManager.AddToRoleAsync(user, "Customer");

            _context.Customers.Add(new Customer { ApplicationUserId = user.Id });
            await _context.SaveChangesAsync();

            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDto> RegisterProviderAsync(RegisterProviderDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return AuthResponseDto.Fail("Email already registered");

            string? profilePicUrl = null;
            if (dto.ProfilePicture != null)
                profilePicUrl = await SaveFileAsync(dto.ProfilePicture, "profiles");

            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = dto.DateOfBirth,
                ProfilePictureUrl = profilePicUrl,
                Role = UserRole.Provider,
                Status = UserStatus.Active
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return AuthResponseDto.Fail(result.Errors.First().Description);

            await _userManager.AddToRoleAsync(user, "Provider");

            AvailabilityStatus availabilityStatus = AvailabilityStatus.Offline;
            if (dto.AvailabilityStatus != null)
                Enum.TryParse(dto.AvailabilityStatus, true, out availabilityStatus);

            var provider = new Provider
            {
                ApplicationUserId = user.Id,
                HourlyRate = dto.HourlyRate,
                ServiceArea = dto.ServiceArea,
                JobTitle = dto.JobTitle,
                ExperienceYears = dto.ExperienceYears,
                Description = dto.Description,
                CurrentLatitude = dto.CurrentLatitude,
                CurrentLongitude = dto.CurrentLongitude,
                State = ProviderState.Pending,
                AvailabilityStatus = availabilityStatus
            };
            _context.Providers.Add(provider);

            if (dto.CertificateImages?.Any() == true)
            {
                foreach (var file in dto.CertificateImages)
                {
                    var url = await SaveFileAsync(file, "certificates");
                    _context.ProviderCertificateImages.Add(new ProviderCertificateImage
                    {
                        ProviderId = user.Id,
                        ImageUrl = url
                    });
                }
            }

            if (dto.PortfolioImages?.Any() == true)
            {
                foreach (var file in dto.PortfolioImages)
                {
                    var url = await SaveFileAsync(file, "portfolio");
                    _context.ProviderPortfolioImages.Add(new ProviderPortfolioImage
                    {
                        ProviderId = user.Id,
                        ImageUrl = url
                    });
                }
            }

            await _context.SaveChangesAsync();
            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
                return AuthResponseDto.Fail("Invalid email or password");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
                return AuthResponseDto.Fail("Invalid email or password");

            if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
                return AuthResponseDto.Fail("Your account is suspended. Please contact support.");

            if (user.IsDeleted)
                return AuthResponseDto.Fail("Your account has been deleted. Please contact support.");

            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDto> AdminLoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
                return AuthResponseDto.Fail("Invalid email or password");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
                return AuthResponseDto.Fail("Invalid email or password");

            if (user.Role != UserRole.Admin)
                return AuthResponseDto.Fail("Access denied");

            if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
                return AuthResponseDto.Fail("Your account is suspended.");

            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var existing = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked && r.Expires > DateTime.UtcNow);

            if (existing == null)
                return AuthResponseDto.Fail("Invalid or expired refresh token");

            existing.IsRevoked = true;
            existing.RevokedAt = DateTime.UtcNow;
            _context.RefreshTokens.Update(existing);
            await _context.SaveChangesAsync();

            return await GenerateTokensAsync(existing.User);
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked);

            if (token == null)
                return false;

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ApiResponse<string>> SendEmailConfirmationAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<string>.NotFound("User not found");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            Console.WriteLine($"[EmailConfirmation] UserId={userId}, Token={token}");

            return ApiResponse<string>.Ok(token, "Confirmation token generated (check console)");
        }

        public async Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<string>.NotFound("User not found");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return ApiResponse<string>.Fail(result.Errors.First().Description);

            return ApiResponse<string>.Ok("Email confirmed successfully");
        }

        private async Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"]!)),
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"]!))
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return AuthResponseDto.Ok(new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = token.ValidTo,
                Role = user.Role.ToString(),
                UserName = user.FullName
            });
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsPath);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }
    }
}
