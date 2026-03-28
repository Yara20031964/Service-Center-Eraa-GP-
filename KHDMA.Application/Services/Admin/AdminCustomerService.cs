using Application.DTOs.Admin;
using Domain.Common;
using KHDMA.Infrastructure.Data;
using KHDMA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using KHDMA.Domain.Enums;

namespace Application.Services.Admin;

public class AdminCustomerService : IAdminCustomerService
{
    private readonly AppDbContext _context;

    public AdminCustomerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<CustomerDto>> GetAllCustomersAsync(
        string? search, int page, int pageSize)
    {
        var query = _context.Users
            .Where(u => u.Role == UserRole.Customer && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email!.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var customers = await query
            .OrderByDescending(u => u.CreateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new CustomerDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.PhoneNumber,
                ProfilePhotoUrl = u.ProfilePictureUrl,
                Status = u.Status,
                CreatedAt = u.CreateAt,
                IsDeleted = u.IsDeleted
            })
            .ToListAsync();

        return PagedResponse<CustomerDto>.Ok(customers, totalCount, page, pageSize);
    }

    public async Task<ApiResponse<CustomerDto>> GetCustomerByIdAsync(string id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                u.Role == UserRole.Customer &&
                !u.IsDeleted);

        if (user is null)
            return ApiResponse<CustomerDto>.NotFound("Customer not found");

        var dto = new CustomerDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.PhoneNumber,
            ProfilePhotoUrl = user.ProfilePictureUrl,
            Status = user.Status,
            CreatedAt = user.CreateAt,
            IsDeleted = user.IsDeleted
        };

        return ApiResponse<CustomerDto>.Ok(dto);
    }

    public async Task<ApiResponse<string>> SuspendCustomerAsync(string id)
    {
        var user = await GetActiveCustomer(id);
        if (user is null)
            return ApiResponse<string>.NotFound("Customer not found");

        if (user.Status == UserStatus.Suspended)
            return ApiResponse<string>.Fail("Customer is already suspended");

        user.Status = UserStatus.Suspended;
        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok("Customer suspended successfully");
    }
    public async Task<ApiResponse<string>> BanCustomerAsync(string id)
    {
        var user = await GetActiveCustomer(id);
        if (user is null)
            return ApiResponse<string>.NotFound("Customer not found");

        if (user.Status == UserStatus.Banned)
            return ApiResponse<string>.Fail("Customer is already banned");

        user.Status = UserStatus.Banned;
        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok("Customer banned successfully");
    }

    public async Task<ApiResponse<string>> RestoreCustomerAsync(string id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                u.Role == UserRole.Customer &&
                !u.IsDeleted);

        if (user is null)
            return ApiResponse<string>.NotFound("Customer not found");

        if (user.Status == UserStatus.Active)
            return ApiResponse<string>.Fail("Customer is already active");

        user.Status = UserStatus.Active;
        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok("Customer restored successfully");
    }

    private async Task<ApplicationUser?> GetActiveCustomer(string id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                u.Role == UserRole.Customer &&
                !u.IsDeleted &&
                u.Status == UserStatus.Active);
    }
}