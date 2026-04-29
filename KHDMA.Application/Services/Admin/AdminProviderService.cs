using Application.DTOs.Admin;
using Domain.Common;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace Application.Services.Admin;

public class AdminProviderService : IAdminProviderService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminProviderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<ProviderApplicationDto>> GetPendingApplicationsAsync(
        int page, int pageSize)
    {
        var all = await _unitOfWork.Repository<ApplicationUser>()
            .GetAsync(
                u => u.Role == UserRole.Provider &&
                     u.Provider != null &&
                     u.Provider.State == ProviderState.Pending &&
                     !u.IsDeleted,
                includes: [u => u.Provider!],
                tracked: false);

        var totalCount = all.Count();

        var items = all
            .OrderBy(u => u.CreateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new ProviderApplicationDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.PhoneNumber,
                ServiceArea = u.Provider!.ServiceArea,
                HourlyRate = u.Provider.HourlyRate ?? 0,
                CreatedAt = u.CreateAt
            });

        return PagedResponse<ProviderApplicationDto>.Ok(items, totalCount, page, pageSize);
    }

    public async Task<ApiResponse<string>> ApproveOrRejectApplicationAsync(
        string id, ApproveRejectDto dto)
    {
        var user = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(
                u => u.Id == id &&
                     u.Role == UserRole.Provider &&
                     u.Provider!.State == ProviderState.Pending &&
                     !u.IsDeleted,
                includes: [u => u.Provider!]);

        if (user is null)
            return ApiResponse<string>.NotFound("Pending provider application not found");

        if (dto.IsApproved)
        {
            user.Provider!.State = ProviderState.Active;
            user.Status = UserStatus.Active;
        }
        else
        {
            user.Provider!.State = ProviderState.Banned;
            user.Status = UserStatus.Banned;
        }

        _unitOfWork.Repository<ApplicationUser>().Update(user);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok(
            dto.IsApproved
                ? "Provider application approved"
                : $"Provider application rejected — {dto.Reason ?? "no reason provided"}");
    }

    public async Task<PagedResponse<ProviderDto>> GetAllProvidersAsync(
        string? search, int page, int pageSize)
    {
        var all = await _unitOfWork.Repository<ApplicationUser>()
            .GetAsync(
                u => u.Role == UserRole.Provider &&
                     u.Provider!.State == ProviderState.Active &&
                     !u.IsDeleted,
                includes: [u => u.Provider!],
                tracked: false);

        if (!string.IsNullOrWhiteSpace(search))
            all = all.Where(u =>
                u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.Email!.Contains(search, StringComparison.OrdinalIgnoreCase));

        var totalCount = all.Count();

        var items = all
            .OrderByDescending(u => u.Provider!.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new ProviderDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.PhoneNumber,
                ProfilePhotoUrl = u.ProfilePictureUrl,
                Status = u.Status,
                ProviderState = u.Provider!.State,
                AvailabilityStatus = u.Provider.AvailabilityStatus,
                ServiceArea = u.Provider.ServiceArea,
                HourlyRate = u.Provider.HourlyRate ?? 0,
                Rating = u.Provider.Rating,
                ReviewCount = u.Provider.ReviewCount,
                CreatedAt = u.CreateAt
            });

        return PagedResponse<ProviderDto>.Ok(items, totalCount, page, pageSize);
    }

    public async Task<ApiResponse<ProviderDto>> GetProviderByIdAsync(string id)
    {
        var u = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(
                x => x.Id == id &&
                     x.Role == UserRole.Provider &&
                     !x.IsDeleted,
                includes: [x => x.Provider!]);

        if (u is null)
            return ApiResponse<ProviderDto>.NotFound("Provider not found");

        return ApiResponse<ProviderDto>.Ok(new ProviderDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.PhoneNumber,
            ProfilePhotoUrl = u.ProfilePictureUrl,
            Status = u.Status,
            ProviderState = u.Provider!.State,
            AvailabilityStatus = u.Provider.AvailabilityStatus,
            ServiceArea = u.Provider.ServiceArea,
            HourlyRate = u.Provider.HourlyRate ?? 0,
            Rating = u.Provider.Rating,
            ReviewCount = u.Provider.ReviewCount,
            CreatedAt = u.CreateAt
        });
    }

    public async Task<ApiResponse<string>> SuspendProviderAsync(string id)
    {
        var u = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(
                x => x.Id == id &&
                     x.Role == UserRole.Provider &&
                     x.Provider!.State == ProviderState.Active &&
                     !x.IsDeleted,
                includes: [x => x.Provider!]);

        if (u is null)
            return ApiResponse<string>.NotFound("Provider not found");

        if (u.Status == UserStatus.Suspended)
            return ApiResponse<string>.Fail("Provider is already suspended");

        u.Status = UserStatus.Suspended;
        u.Provider!.State = ProviderState.Suspended;
        u.Provider.AvailabilityStatus = AvailabilityStatus.Offline;

        _unitOfWork.Repository<ApplicationUser>().Update(u);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Provider suspended successfully");
    }

    public async Task<ApiResponse<string>> BanProviderAsync(string id)
    {
        var u = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(
                x => x.Id == id &&
                     x.Role == UserRole.Provider &&
                     !x.IsDeleted,
                includes: [x => x.Provider!]);

        if (u is null)
            return ApiResponse<string>.NotFound("Provider not found");

        if (u.Status == UserStatus.Banned)
            return ApiResponse<string>.Fail("Provider is already banned");

        u.Status = UserStatus.Banned;
        u.Provider!.State = ProviderState.Banned;
        u.Provider.AvailabilityStatus = AvailabilityStatus.Offline;

        _unitOfWork.Repository<ApplicationUser>().Update(u);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Provider banned successfully");
    }

    public async Task<ApiResponse<string>> RestoreProviderAsync(string id)
    {
        var u = await _unitOfWork.Repository<ApplicationUser>()
            .GetOneAsync(
                x => x.Id == id &&
                     x.Role == UserRole.Provider &&
                     !x.IsDeleted,
                includes: [x => x.Provider!]);

        if (u is null)
            return ApiResponse<string>.NotFound("Provider not found");

        if (u.Status == UserStatus.Active)
            return ApiResponse<string>.Fail("Provider is already active");

        u.Status = UserStatus.Active;
        u.Provider!.State = ProviderState.Active;

        _unitOfWork.Repository<ApplicationUser>().Update(u);
        await _unitOfWork.CommitAsync();

        return ApiResponse<string>.Ok("Provider restored successfully");
    }
}