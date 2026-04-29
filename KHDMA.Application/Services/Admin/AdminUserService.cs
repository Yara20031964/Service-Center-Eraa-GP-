using Application.DTOs.Admin;
using Domain.Common;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<PagedResponse<AdminUserDto>> GetAllAdminsAsync(
        string? search, int page, int pageSize)
    {
        var all = await _unitOfWork.Repository<ApplicationUser>()
            .GetAsync(u => u.Role == UserRole.Admin && !u.IsDeleted, tracked: false);

        if (!string.IsNullOrWhiteSpace(search))
            all = all.Where(u =>
                u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.Email!.Contains(search, StringComparison.OrdinalIgnoreCase));

        var totalCount = all.Count();

        var items = all
            .OrderByDescending(u => u.CreateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                PhoneNumber = u.PhoneNumber,
                ProfilePictureUrl = u.ProfilePictureUrl,
                Status = u.Status,
                CreateAt = u.CreateAt
            });

        return PagedResponse<AdminUserDto>.Ok(items, totalCount, page, pageSize);
    }

    public async Task<ApiResponse<AdminUserDto>> GetAdminByIdAsync(string id)
    {
        var user = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(u => u.Id == id &&
                              u.Role == UserRole.Admin &&
                              !u.IsDeleted);

        if (user is null)
            return ApiResponse<AdminUserDto>.NotFound("Admin not found");

        return ApiResponse<AdminUserDto>.Ok(new AdminUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Status = user.Status,
            CreateAt = user.CreateAt
        });
    }

    public async Task<ApiResponse<AdminUserDto>> CreateAdminAsync(CreateAdminDto dto)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
            return ApiResponse<AdminUserDto>.Fail("Email is already in use");

        var user = new ApplicationUser
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            CreateAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResponse<AdminUserDto>.Fail(errors);
        }

        return ApiResponse<AdminUserDto>.Created(new AdminUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            CreateAt = user.CreateAt
        });
    }

    public async Task<ApiResponse<string>> DeactivateAdminAsync(string id)
    {
        var user = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(u => u.Id == id &&
                              u.Role == UserRole.Admin &&
                              !u.IsDeleted);

        if (user is null)
            return ApiResponse<string>.NotFound("Admin not found");

        if (user.Status == UserStatus.Suspended)
            return ApiResponse<string>.Fail("Admin is already deactivated");

        user.Status = UserStatus.Suspended;
        _unitOfWork.Repository<ApplicationUser>().Update(user);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Admin deactivated successfully");
    }
}