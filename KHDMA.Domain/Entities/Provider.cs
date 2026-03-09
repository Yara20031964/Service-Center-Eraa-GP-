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

        //Navigation properties
        public ApplicationUser ApplicationUser { get; set; }
        public ICollection<ProviderService> ProviderServices { get; set; } = new List<ProviderService>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

    }
}