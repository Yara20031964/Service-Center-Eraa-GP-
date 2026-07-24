using Microsoft.AspNetCore.Http;

namespace KHDMA.Application.DTOs.Profile;

public class BaseProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
}

public class CustomerProfileDto : BaseProfileDto { }

public class ProviderProfileDto : BaseProfileDto
{
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string? ServiceArea { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? JobTitle { get; set; }
    public int? ExperienceYears { get; set; }
    public string? Description { get; set; }
    public int NumberOfJobsDone { get; set; }
    public string State { get; set; } = string.Empty;
    public string AvailabilityStatus { get; set; } = string.Empty;

    /// <summary>The point dispatch matches this provider against.</summary>
    public double? WorkingLatitude { get; set; }
    public double? WorkingLongitude { get; set; }

    /// <summary>Last live position, which moves while a job is en route.</summary>
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
}

public class AdminProfileDto : BaseProfileDto { }

public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public IFormFile? ProfilePicture { get; set; }
    public string? ServiceArea { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? JobTitle { get; set; }
    public int? ExperienceYears { get; set; }
    public string? Description { get; set; }
    public string? AvailabilityStatus { get; set; }

    /// <summary>
    /// Moves the working point. Keeps the wire name the client already sends; it
    /// has never meant "where I am standing", only "where I work".
    /// </summary>
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
}

public class ChangePasswordDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
