using Microsoft.AspNetCore.Http;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Auth.Request
{
    public class BaseRegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public UserRole Role { get; set; }
    }
}
