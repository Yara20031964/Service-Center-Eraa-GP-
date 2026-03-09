namespace KHDMA.Domain.Entities
{
    public class Service
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public Guid CategoryId { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public decimal? FixedPrice { get; set; }
        public int? EstimatedDurationMin { get; set; }
        public int? EstimatedDurationMax { get; set; }
        public bool IsActive { get; set; } = true;

        //Navigation properties
        public Category Category { get; set; }
        public ICollection<ProviderService> ProviderServices { get; set; } = new List<ProviderService>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    }
}