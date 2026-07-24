using System.ComponentModel.DataAnnotations;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Provider;

/// <summary>
/// A dispatch card recovered over REST after a provider reconnects.
/// Deliberately contains distance only; the booking address is revealed on accept.
/// </summary>
public class PendingJobDto
{
    public Guid BookingId { get; set; }
    public string ServiceNameEn { get; set; } = string.Empty;
    public string ServiceNameAr { get; set; } = string.Empty;
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameAr { get; set; } = string.Empty;
    public string CustomerFirstName { get; set; } = string.Empty;
    public string? CustomerAvatarUrl { get; set; }
    public double DistanceKm { get; set; }
    public decimal ProviderEarning { get; set; }
    public string Currency { get; set; } = "EGP";
    public int? EstimatedDurationMin { get; set; }
    public int? EstimatedDurationMax { get; set; }
    public string BookingType { get; set; } = nameof(Domain.Enums.BookingType.Immediate);
    public DateTime? ScheduledTime { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int SecondsRemaining { get; set; }
}

public class UpdateAvailabilityDto
{
    [Required]
    public AvailabilityStatus? Status { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class ProviderAvailabilityDto
{
    public AvailabilityStatus Status { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
/// One catalogue service, flagged with whether this provider offers it.
/// </summary>
/// <remarks>
/// The whole active catalogue is returned rather than only the provider's own
/// rows, so the editor needs a single call: the client filters on
/// <see cref="IsOffered"/> to display and toggles it to edit. Splitting this
/// would leave the client reconciling two sets itself.
/// </remarks>
public class ProviderServiceDto
{
    public Guid ServiceId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameAr { get; set; } = string.Empty;
    public string? Image { get; set; }
    public decimal? FixedPrice { get; set; }
    public bool IsOffered { get; set; }
}

/// <summary>Replaces the provider's offered set with exactly these services.</summary>
public class UpdateProviderServicesDto
{
    [Required]
    public List<Guid> ServiceIds { get; set; } = [];
}
