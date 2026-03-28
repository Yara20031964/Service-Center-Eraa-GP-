using KHDMA.Domain.Enums;

namespace Application.DTOs.Admin;

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreateAt { get; set; }
}

public class CreateAdminDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}