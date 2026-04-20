namespace KHDMA.Domain.Entities
{
    public class Service
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public Guid CategoryId { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public decimal? FixedPrice { get; set; }
        public int? EstimatedDurationMin { get; set; }
        public int? EstimatedDurationMax { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal Rating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;

        //Navigation properties
        public Category Category { get; set; }
        public ICollection<ProviderService> ProviderServices { get; set; } = new List<ProviderService>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<ServiceImage> Images { get; set; } = new List<ServiceImage>();
        public ICollection<CustomerFavorite> Favorites { get; set; } = new List<CustomerFavorite>();

    }
}