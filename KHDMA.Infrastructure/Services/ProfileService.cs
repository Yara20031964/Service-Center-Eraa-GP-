using Domain.Common;
using KHDMA.Application.DTOs.Profile;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProfileService(UserManager<ApplicationUser> userManager, AppDbContext context, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _context = context;
        _env = env;
    }

    public async Task<ApiResponse<object>> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ApiResponse<object>.NotFound("User not found");

        if (user.Role == UserRole.Provider)
        {
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            return ApiResponse<object>.Ok(MapProviderProfile(user, provider));
        }

        if (user.Role == UserRole.Admin)
            return ApiResponse<object>.Ok(MapBaseProfile<AdminProfileDto>(user));

        return ApiResponse<object>.Ok(MapBaseProfile<CustomerProfileDto>(user));
    }

    public async Task<ApiResponse<object>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ApiResponse<object>.NotFound("User not found");

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.DateOfBirth.HasValue) user.DateOfBirth = dto.DateOfBirth.Value;

        if (dto.Email != null && dto.Email != user.Email)
        {
            user.Email = dto.Email;
            user.NormalizedEmail = dto.Email.ToUpperInvariant();
            user.UserName = dto.Email;
            user.NormalizedUserName = dto.Email.ToUpperInvariant();
        }

        if (dto.ProfilePicture != null)
            user.ProfilePictureUrl = await SaveFileAsync(dto.ProfilePicture, "profiles");

        await _userManager.UpdateAsync(user);

        if (user.Role == UserRole.Provider)
        {
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            if (provider != null)
            {
                if (dto.ServiceArea != null) provider.ServiceArea = dto.ServiceArea;
                if (dto.HourlyRate.HasValue) provider.HourlyRate = dto.HourlyRate.Value;
                if (dto.JobTitle != null) provider.JobTitle = dto.JobTitle;
                if (dto.ExperienceYears.HasValue) provider.ExperienceYears = dto.ExperienceYears.Value;
                if (dto.Description != null) provider.Description = dto.Description;
                if (dto.CurrentLatitude.HasValue) provider.CurrentLatitude = dto.CurrentLatitude.Value;
                if (dto.CurrentLongitude.HasValue) provider.CurrentLongitude = dto.CurrentLongitude.Value;

                if (dto.AvailabilityStatus != null &&
                    Enum.TryParse<AvailabilityStatus>(dto.AvailabilityStatus, true, out var status))
                    provider.AvailabilityStatus = status;

                await _context.SaveChangesAsync();
                return ApiResponse<object>.Ok(MapProviderProfile(user, provider));
            }
        }

        return await GetProfileAsync(userId);
    }

    public async Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ApiResponse<string>.NotFound("User not found");

        var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
        if (!result.Succeeded)
            return ApiResponse<string>.Fail(result.Errors.First().Description);

        return ApiResponse<string>.Ok("Password changed successfully");
    }

    public async Task<ApiResponse<List<AddressDto>>> GetAddressesAsync(string userId)
    {
        var addresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .Select(a => new AddressDto
            {
                Id = a.Id,
                Label = a.Label,
                AddresssLine = a.AddresssLine,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            })
            .ToListAsync();

        return ApiResponse<List<AddressDto>>.Ok(addresses);
    }

    public async Task<ApiResponse<AddressDto>> AddAddressAsync(string userId, CreateAddressDto dto)
    {
        var address = new Address
        {
            UserId = userId,
            Label = dto.Label,
            AddresssLine = dto.AddresssLine,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        return ApiResponse<AddressDto>.Created(new AddressDto
        {
            Id = address.Id,
            Label = address.Label,
            AddresssLine = address.AddresssLine,
            Latitude = address.Latitude,
            Longitude = address.Longitude
        });
    }

    public async Task<ApiResponse<string>> DeleteAddressAsync(string userId, Guid addressId)
    {
        var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
        if (address == null)
            return ApiResponse<string>.NotFound("Address not found");

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok("Address deleted");
    }

    public async Task<ApiResponse<List<string>>> GetCertificateImagesAsync(string userId)
    {
        var urls = await _context.ProviderCertificateImages
            .Where(i => i.ProviderId == userId)
            .Select(i => i.ImageUrl)
            .ToListAsync();

        return ApiResponse<List<string>>.Ok(urls);
    }

    public async Task<ApiResponse<List<string>>> AddCertificateImagesAsync(string userId, List<IFormFile> images)
    {
        var urls = new List<string>();
        foreach (var file in images)
        {
            var url = await SaveFileAsync(file, "certificates");
            _context.ProviderCertificateImages.Add(new ProviderCertificateImage { ProviderId = userId, ImageUrl = url });
            urls.Add(url);
        }
        await _context.SaveChangesAsync();
        return ApiResponse<List<string>>.Ok(urls);
    }

    public async Task<ApiResponse<string>> DeleteCertificateImageAsync(string userId, Guid imageId)
    {
        var image = await _context.ProviderCertificateImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProviderId == userId);
        if (image == null)
            return ApiResponse<string>.NotFound("Image not found");

        _context.ProviderCertificateImages.Remove(image);
        await _context.SaveChangesAsync();
        return ApiResponse<string>.Ok("Image deleted");
    }

    public async Task<ApiResponse<List<string>>> GetPortfolioImagesAsync(string userId)
    {
        var urls = await _context.ProviderPortfolioImages
            .Where(i => i.ProviderId == userId)
            .Select(i => i.ImageUrl)
            .ToListAsync();

        return ApiResponse<List<string>>.Ok(urls);
    }

    public async Task<ApiResponse<List<string>>> AddPortfolioImagesAsync(string userId, List<IFormFile> images)
    {
        var urls = new List<string>();
        foreach (var file in images)
        {
            var url = await SaveFileAsync(file, "portfolio");
            _context.ProviderPortfolioImages.Add(new ProviderPortfolioImage { ProviderId = userId, ImageUrl = url });
            urls.Add(url);
        }
        await _context.SaveChangesAsync();
        return ApiResponse<List<string>>.Ok(urls);
    }

    public async Task<ApiResponse<string>> DeletePortfolioImageAsync(string userId, Guid imageId)
    {
        var image = await _context.ProviderPortfolioImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProviderId == userId);
        if (image == null)
            return ApiResponse<string>.NotFound("Image not found");

        _context.ProviderPortfolioImages.Remove(image);
        await _context.SaveChangesAsync();
        return ApiResponse<string>.Ok("Image deleted");
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

    private static T MapBaseProfile<T>(ApplicationUser user) where T : BaseProfileDto, new() => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        PhoneNumber = user.PhoneNumber,
        ProfilePictureUrl = user.ProfilePictureUrl,
        DateOfBirth = user.DateOfBirth,
        Role = user.Role.ToString(),
        EmailConfirmed = user.EmailConfirmed
    };

    private static ProviderProfileDto MapProviderProfile(ApplicationUser user, Provider? provider) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        PhoneNumber = user.PhoneNumber,
        ProfilePictureUrl = user.ProfilePictureUrl,
        DateOfBirth = user.DateOfBirth,
        Role = user.Role.ToString(),
        EmailConfirmed = user.EmailConfirmed,
        Rating = provider?.Rating ?? 0,
        ReviewCount = provider?.ReviewCount ?? 0,
        ServiceArea = provider?.ServiceArea,
        HourlyRate = provider?.HourlyRate,
        JobTitle = provider?.JobTitle,
        ExperienceYears = provider?.ExperienceYears,
        Description = provider?.Description,
        NumberOfJobsDone = provider?.NumberOfJobsDone ?? 0,
        State = provider?.State.ToString() ?? string.Empty,
        AvailabilityStatus = provider?.AvailabilityStatus.ToString() ?? string.Empty,
        CurrentLatitude = provider?.CurrentLatitude,
        CurrentLongitude = provider?.CurrentLongitude
    };
}
