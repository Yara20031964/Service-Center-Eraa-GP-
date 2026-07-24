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
        /// <summary>
        /// Where the provider is <em>right now</em>. Written by live tracking while a
        /// job is en route, so it moves constantly and is only meaningful to the
        /// customer watching the map.
        /// </summary>
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }

        /// <summary>
        /// The point the provider chose to be available at, and the only one dispatch
        /// matches against.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="CurrentLatitude"/> on purpose. While these shared
        /// one pair of columns, the en-route GPS stream overwrote the chosen point, so
        /// finishing a job silently re-homed the provider to the customer's doorstep.
        /// Only going online and editing the profile write this.
        /// </remarks>
        public double? WorkingLatitude { get; set; }
        public double? WorkingLongitude { get; set; }

        /// <summary>
        /// When this provider was last heard from - either endpoint refreshes it.
        /// Dispatch treats it as a liveness signal rather than a position: an app
        /// force-closed while Online stops winning the nearest-first sort instead of
        /// silently burning a dispatch round each time.
        /// </summary>
        public DateTime? LocationUpdatedAt { get; set; }
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