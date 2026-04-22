using Microsoft.AspNetCore.Http;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Auth.Request
{
    public class RegisterProviderDto : BaseRegisterDto
    {
        public decimal? HourlyRate { get; set; }
        public string? ServiceArea { get; set; }
        public string? JobTitle { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Description { get; set; }
        public string? AvailabilityStatus { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public List<IFormFile>? CertificateImages { get; set; }
        public List<IFormFile>? PortfolioImages { get; set; }

        public RegisterProviderDto()
        {
            Role = UserRole.Provider;
        }
    }
}
