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

        if (provider.CurrentLatitude is null || provider.CurrentLongitude is null)
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
                    provider.CurrentLatitude.Value,
                    provider.CurrentLongitude.Value,
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
            provider.CurrentLatitude = dto.Latitude.Value;
            provider.CurrentLongitude = dto.Longitude.Value;
        }

        await _db.SaveChangesAsync();

        return ApiResponse<ProviderAvailabilityDto>.Ok(new ProviderAvailabilityDto
        {
            Status = provider.AvailabilityStatus,
            Latitude = provider.CurrentLatitude,
            Longitude = provider.CurrentLongitude,
        }, "Availability updated");
    }

    private static string FirstName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var space = fullName.IndexOf(' ');
        return space < 0 ? fullName : fullName[..space];
    }
}
