using KHDMA.Domain.Enums;

namespace Application.DTOs.Admin;

public class CustomerDto
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public UserStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }
}