using Domain.Common;
using KHDMA.Application.DTOs.Provider;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services;

public class ProviderJobsService : IProviderJobsService
{
    private readonly AppDbContext _db;
    private readonly IDispatchCandidateStore _candidates;
    private readonly IPricingService _pricing;
    private readonly IImageUrlResolver _imageUrlResolver;

    public ProviderJobsService(
        AppDbContext db,
        IDispatchCandidateStore candidates,
        IPricingService pricing,
        IImageUrlResolver imageUrlResolver)
    {
        _db = db;
        _candidates = candidates;
        _pricing = pricing;
        _imageUrlResolver = imageUrlResolver;
    }

    public async Task<ApiResponse<List<PendingJobDto>>> GetPendingJobsAsync(string providerId)
    {
        var provider = await _db.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == providerId);

        if (provider is null)
            return ApiResponse<List<PendingJobDto>>.NotFound("Provider not found");

        // The working point, not the live one: the card's distance should be the
        // distance dispatch actually matched on.
        var providerLat = provider.WorkingLatitude;
        var providerLng = provider.WorkingLongitude;
        if (providerLat is null || providerLng is null)
            return ApiResponse<List<PendingJobDto>>.Ok([]);

        var now = DateTime.UtcNow;
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Service)
                .ThenInclude(s => s.Category)
            .Include(b => b.Customer)
                .ThenInclude(c => c.ApplicationUser)
            .Include(b => b.Payment)
            .Where(b => b.Status == BookingStatus.Dispatching
                     && b.ProviderId == null
                     && b.DispatchDeadline != null
                     && b.DispatchDeadline > now)
            .OrderBy(b => b.DispatchDeadline)
            .ToListAsync();

        var cards = new List<PendingJobDto>();

        foreach (var booking in bookings)
        {
            var candidateIds = await _candidates.GetAsync(booking.Id);
            if (!candidateIds.Contains(providerId, StringComparer.Ordinal))
                continue;

            // A booking without coordinates cannot enter dispatch. Ignore a
            // malformed stale candidate rather than exposing an invented distance.
            if (booking.Latitude is null || booking.Longitude is null)
                continue;

            var serviceFee = booking.Payment?.ServiceFee > 0
                ? booking.Payment.ServiceFee
                : booking.Service?.FixedPrice ?? booking.TotalPrice;
            var price = await _pricing.ForServiceFeeAsync(serviceFee);
            var deadline = booking.DispatchDeadline!.Value;
            var customerName = booking.Customer?.ApplicationUser?.FullName ?? string.Empty;

            cards.Add(new PendingJobDto
            {
                BookingId = booking.Id,
                ServiceNameEn = booking.Service?.NameEn ?? string.Empty,
                ServiceNameAr = booking.Service?.NameAr ?? string.Empty,
                CategoryNameEn = booking.Service?.Category?.NameEn ?? string.Empty,
                CategoryNameAr = booking.Service?.Category?.NameAr ?? string.Empty,
                CustomerFirstName = FirstName(customerName),
                CustomerAvatarUrl = _imageUrlResolver.Resolve(
                    booking.Customer?.ApplicationUser?.ProfilePictureUrl),
                DistanceKm = Math.Round(DispatchService.Haversine(
                    providerLat.Value,
                    providerLng.Value,
                    booking.Latitude.Value,
                    booking.Longitude.Value), 2),
                ProviderEarning = price.ProviderEarning,
                Currency = price.Currency,
                EstimatedDurationMin = booking.Service?.EstimatedDurationMin,
                EstimatedDurationMax = booking.Service?.EstimatedDurationMax,
                BookingType = booking.BookingType.ToString(),
                ScheduledTime = booking.ScheduledTime,
                ExpiresAt = deadline,
                SecondsRemaining = Math.Max(0, (int)(deadline - DateTime.UtcNow).TotalSeconds),
            });
        }

        return ApiResponse<List<PendingJobDto>>.Ok(cards);
    }

    public async Task<ApiResponse<ProviderAvailabilityDto>> UpdateAvailabilityAsync(
        string providerId, UpdateAvailabilityDto dto)
    {
        if (dto.Status is null || !Enum.IsDefined(dto.Status.Value))
            return ApiResponse<ProviderAvailabilityDto>.Fail("A valid availability status is required");

        var provider = await _db.Providers
            .FirstOrDefaultAsync(p => p.ApplicationUserId == providerId);

        if (provider is null)
            return ApiResponse<ProviderAvailabilityDto>.NotFound("Provider not found");

        provider.AvailabilityStatus = dto.Status.Value;

        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            // Going online is one of only two places the working point moves; live
            // tracking must never land here or a job would relocate the provider.
            provider.WorkingLatitude = dto.Latitude.Value;
            provider.WorkingLongitude = dto.Longitude.Value;
            provider.LocationUpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return ApiResponse<ProviderAvailabilityDto>.Ok(new ProviderAvailabilityDto
        {
            Status = provider.AvailabilityStatus,
            Latitude = provider.WorkingLatitude,
            Longitude = provider.WorkingLongitude,
        }, "Availability updated");
    }

    private static string FirstName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var space = fullName.IndexOf(' ');
        return space < 0 ? fullName : fullName[..space];
    }

    public async Task<ApiResponse<List<ProviderServiceDto>>> GetServicesAsync(string providerId)
    {
        if (!await _db.Providers.AnyAsync(p => p.ApplicationUserId == providerId))
            return ApiResponse<List<ProviderServiceDto>>.NotFound("Provider not found");

        return ApiResponse<List<ProviderServiceDto>>.Ok(await CatalogueForAsync(providerId));
    }

    public async Task<ApiResponse<List<ProviderServiceDto>>> UpdateServicesAsync(
        string providerId, UpdateProviderServicesDto dto)
    {
        if (!await _db.Providers.AnyAsync(p => p.ApplicationUserId == providerId))
            return ApiResponse<List<ProviderServiceDto>>.NotFound("Provider not found");

        var requested = dto.ServiceIds.Distinct().ToList();

        // Reject unknown or retired services rather than silently dropping them,
        // so a stale client cannot leave the provider believing they offer
        // something dispatch will never match them on.
        var valid = await _db.Services
            .AsNoTracking()
            .Where(s => requested.Contains(s.id) && s.IsActive)
            .Select(s => s.id)
            .ToListAsync();

        if (valid.Count != requested.Count)
            return ApiResponse<List<ProviderServiceDto>>.Fail(
                "One or more of the selected services is unavailable");

        var existing = await _db.ProviderServices
            .Where(ps => ps.ProviderId == providerId)
            .ToListAsync();

        // Rows are reactivated rather than recreated so the original CreateAt
        // survives a provider toggling a service off and back on.
        foreach (var row in existing)
            row.IsActive = valid.Contains(row.ServiceId);

        var known = existing.Select(ps => ps.ServiceId).ToHashSet();
        foreach (var serviceId in valid.Where(id => !known.Contains(id)))
        {
            _db.ProviderServices.Add(new Domain.Entities.ProviderService
            {
                ProviderId = providerId,
                ServiceId = serviceId,
                IsActive = true,
            });
        }

        await _db.SaveChangesAsync();

        return ApiResponse<List<ProviderServiceDto>>.Ok(
            await CatalogueForAsync(providerId), "Services updated");
    }

    private async Task<List<ProviderServiceDto>> CatalogueForAsync(string providerId)
    {
        var offered = await _db.ProviderServices
            .AsNoTracking()
            .Where(ps => ps.ProviderId == providerId && ps.IsActive)
            .Select(ps => ps.ServiceId)
            .ToListAsync();

        var services = await _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Category.NameEn)
            .ThenBy(s => s.NameEn)
            .Select(s => new ProviderServiceDto
            {
                ServiceId = s.id,
                NameEn = s.NameEn,
                NameAr = s.NameAr,
                CategoryNameEn = s.Category.NameEn,
                CategoryNameAr = s.Category.NameAr,
                Image = s.Image,
                FixedPrice = s.FixedPrice,
            })
            .ToListAsync();

        // Resolved in memory: the URL resolver is not translatable to SQL.
        foreach (var service in services)
        {
            service.Image = _imageUrlResolver.Resolve(service.Image);
            service.IsOffered = offered.Contains(service.ServiceId);
        }

        return services;
    }
}
