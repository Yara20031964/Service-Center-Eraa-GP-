using Application.DTOs.Admin;
using Domain.Common;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ── GET ALL ───────────────────────────────────────────────
    public async Task<PagedResponse<AdminUserDto>> GetAllAdminsAsync(
        string? search, int page, int pageSize)
    {
        var query = _context.Users
            .Where(u => u.Role == UserRole.Admin && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email!.Contains(search));

        var totalCount = await query.CountAsync();

        var items = await query
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
            })
            .ToListAsync();

        return PagedResponse<AdminUserDto>.Ok(items, totalCount, page, pageSize);
    }

    // ── GET BY ID ─────────────────────────────────────────────
    public async Task<ApiResponse<AdminUserDto>> GetAdminByIdAsync(string id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id &&
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

    // ── CREATE ────────────────────────────────────────────────
    public async Task<ApiResponse<AdminUserDto>> CreateAdminAsync(CreateAdminDto dto)
    {
        var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingEmail is not null)
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

    // ── DEACTIVATE ────────────────────────────────────────────
    public async Task<ApiResponse<string>> DeactivateAdminAsync(string id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id &&
                                      u.Role == UserRole.Admin &&
                                      !u.IsDeleted);

        if (user is null)
            return ApiResponse<string>.NotFound("Admin not found");

        if (user.Status == UserStatus.Suspended)
            return ApiResponse<string>.Fail("Admin is already deactivated");

        user.Status = UserStatus.Suspended;
        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok("Admin deactivated successfully");
    }
}