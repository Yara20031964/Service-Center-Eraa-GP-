using Application.DTOs.Admin;
using Domain.Common;
using KHDMA.Domain.Entities;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using KHDMA.Domain.Enums;

namespace Application.Services.Admin;

public class AdminProviderService : IAdminProviderService
{
    private readonly AppDbContext _context;

    public AdminProviderService(AppDbContext context)
    {
        _context = context;
    }

    // ── PENDING APPLICATIONS ──────────────────────────────────
    public async Task<PagedResponse<ProviderApplicationDto>> GetPendingApplicationsAsync(
        int page, int pageSize)
    {
        var query = _context.Users
            .Include(u => u.Provider)
            .Where(u => u.Role == UserRole.Provider &&
                        u.Provider != null &&
                        u.Provider.State == ProviderState.Pending &&
                        !u.IsDeleted);

        var totalCount = await query.CountAsync();

        var items = await query
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
            })
            .ToListAsync();

        return PagedResponse<ProviderApplicationDto>.Ok(items, totalCount, page, pageSize);
    }

    // ── APPROVE / REJECT ──────────────────────────────────────
    public async Task<ApiResponse<string>> ApproveOrRejectApplicationAsync(
        string id, ApproveRejectDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Provider)
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                u.Role == UserRole.Provider &&
                u.Provider!.State == ProviderState.Pending &&
                !u.IsDeleted);

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

        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok(
            dto.IsApproved
                ? "Provider application approved"
                : $"Provider application rejected — {dto.Reason ?? "no reason provided"}");
    }

    // ── GET ALL ACTIVE PROVIDERS ──────────────────────────────
    public async Task<PagedResponse<ProviderDto>> GetAllProvidersAsync(
        string? search, int page, int pageSize)
    {
        var query = _context.Users
            .Include(u => u.Provider)
            .Where(u => u.Role == UserRole.Provider &&
                        u.Provider!.State == ProviderState.Active &&
                        !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email!.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
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
            })
            .ToListAsync();

        return PagedResponse<ProviderDto>.Ok(items, totalCount, page, pageSize);
    }

    // ── GET BY ID ─────────────────────────────────────────────
    public async Task<ApiResponse<ProviderDto>> GetProviderByIdAsync(string id)
    {
        var u = await _context.Users
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Role == UserRole.Provider &&
                !x.IsDeleted);

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
        var u = await GetProvider(id);
        if (u is null)
            return ApiResponse<string>.NotFound("Provider not found");

        if (u.Status == UserStatus.Suspended)
            return ApiResponse<string>.Fail("Provider is already suspended");

        u.Status = UserStatus.Suspended;
        u.Provider!.State = ProviderState.Suspended;
        u.Provider.AvailabilityStatus = AvailabilityStatus.Offline;

        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok("Provider suspended successfully");
    }

    public async Task<ApiResponse<string>> BanProviderAsync(string id)
    {
        var u = await GetProvider(id);
        if (u is null)
            return ApiResponse<string>.NotFound("Provider not found");

        if (u.Status == UserStatus.Banned)
            return ApiResponse<string>.Fail("Provider is already banned");

        u.Status = UserStatus.Banned;
        u.Provider!.State = ProviderState.Banned;
        u.Provider.AvailabilityStatus = AvailabilityStatus.Offline;

        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok("Provider banned successfully");
    }
    
    public async Task<ApiResponse<string>> RestoreProviderAsync(string id)
    {
        var u = await _context.Users
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Role == UserRole.Provider &&
                !x.IsDeleted);

        if (u is null)
            return ApiResponse<string>.NotFound("Provider not found");

        if (u.Status == UserStatus.Active)
            return ApiResponse<string>.Fail("Provider is already active");

        u.Status = UserStatus.Active;
        u.Provider!.State = ProviderState.Active;

        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok("Provider restored successfully");
    }

    private async Task<ApplicationUser?> GetProvider(string id)
    {
        return await _context.Users
            .Include(u => u.Provider)
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                u.Role == UserRole.Provider &&
                u.Provider!.State == ProviderState.Active &&
                !u.IsDeleted);
    }
}