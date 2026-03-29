using Application.DTOs.Admin;
using Domain.Common;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace Application.Services.Admin;

public class AdminCustomerService : IAdminCustomerService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminCustomerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<CustomerDto>> GetAllCustomersAsync(
        string? search, int page, int pageSize)
    {
        var all = await _unitOfWork.Repository<ApplicationUser>()
            .GetAsync(u => u.Role == UserRole.Customer && !u.IsDeleted, tracked: false);

        if (!string.IsNullOrWhiteSpace(search))
            all = all.Where(u =>
                u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.Email!.Contains(search, StringComparison.OrdinalIgnoreCase));

        var totalCount = all.Count();

        var items = all
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
            });

        return PagedResponse<CustomerDto>.Ok(items, totalCount, page, pageSize);
    }

    public async Task<ApiResponse<CustomerDto>> GetCustomerByIdAsync(string id)
    {
        var user = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(u => u.Id == id &&
                              u.Role == UserRole.Customer &&
                              !u.IsDeleted);

        if (user is null)
            return ApiResponse<CustomerDto>.NotFound("Customer not found");

        return ApiResponse<CustomerDto>.Ok(new CustomerDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.PhoneNumber,
            ProfilePhotoUrl = user.ProfilePictureUrl,
            Status = user.Status,
            CreatedAt = user.CreateAt,
            IsDeleted = user.IsDeleted
        });
    }

    public async Task<ApiResponse<string>> SuspendCustomerAsync(string id)
    {
        var user = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(u => u.Id == id &&
                              u.Role == UserRole.Customer &&
                              u.Status == UserStatus.Active &&
                              !u.IsDeleted);

        if (user is null)
            return ApiResponse<string>.NotFound("Customer not found");

        if (user.Status == UserStatus.Suspended)
            return ApiResponse<string>.Fail("Customer is already suspended");

        user.Status = UserStatus.Suspended;
        _unitOfWork.Repository<ApplicationUser>().Update(user);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Customer suspended successfully");
    }

    public async Task<ApiResponse<string>> BanCustomerAsync(string id)
    {
        var user = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(u => u.Id == id &&
                              u.Role == UserRole.Customer &&
                              !u.IsDeleted);

        if (user is null)
            return ApiResponse<string>.NotFound("Customer not found");

        if (user.Status == UserStatus.Banned)
            return ApiResponse<string>.Fail("Customer is already banned");

        user.Status = UserStatus.Banned;
        _unitOfWork.Repository<ApplicationUser>().Update(user);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Customer banned successfully");
    }

    public async Task<ApiResponse<string>> RestoreCustomerAsync(string id)
    {
        var user = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(u => u.Id == id &&
                              u.Role == UserRole.Customer &&
                              !u.IsDeleted);

        if (user is null)
            return ApiResponse<string>.NotFound("Customer not found");

        if (user.Status == UserStatus.Active)
            return ApiResponse<string>.Fail("Customer is already active");

        user.Status = UserStatus.Active;
        _unitOfWork.Repository<ApplicationUser>().Update(user);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Customer restored successfully");
    }
}