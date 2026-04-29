using KHDMA.Domain.Enums;

namespace Application.DTOs.Admin;

public class ProviderDto
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public UserStatus Status { get; set; }

    public ProviderState ProviderState { get; set; }

    public AvailabilityStatus AvailabilityStatus { get; set; }

    public string? ServiceArea { get; set; }

    public decimal HourlyRate { get; set; }

    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public double PublicRating => ReviewCount >= 3 ? Rating : 0;

    public DateTime CreatedAt { get; set; }
}

public class ProviderApplicationDto
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? ServiceArea { get; set; }

    public decimal HourlyRate { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class ApproveRejectDto
{
    public bool IsApproved { get; set; }

    public string? Reason { get; set; }
}