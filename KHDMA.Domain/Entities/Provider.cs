using KHDMA.Domain.Enums;

namespace KHDMA.Domain.Entities
{
    public class Provider
    {
        public string ApplicationUserId { get; set; }
        public double Rating { get; set; } = 0.0;
        public int ReviewCount { get; set; } = 0;
        public ProviderState State { get; set; } = ProviderState.Pending;
        public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Offline;
        public string? ServiceArea { get; set; }
        public decimal? HourlyRate { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public string? JobTitle { get; set; }
        public int? ExperienceYears { get; set; }
        public int NumberOfJobsDone { get; set; } = 0;
        public decimal TotalEarnings { get; set; } = 0;
        public decimal Balance { get; set; } = 0;
        public string? Description { get; set; }

        //Navigation properties
        public ApplicationUser ApplicationUser { get; set; }
        public ICollection<ProviderService> ProviderServices { get; set; } = new List<ProviderService>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<CustomerFavoriteProvider> FavoritedBy { get; set; } = new List<CustomerFavoriteProvider>();
        public ICollection<ProviderPortfolioImage> PortfolioImages { get; set; } = new List<ProviderPortfolioImage>();
        public ICollection<ProviderCertificateImage> CertificateImages { get; set; } = new List<ProviderCertificateImage>();
    }
}