namespace KHDMA.Domain.Entities
{
    public class Review
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BookingId { get; set; }
        public string CustomerId { get; set; }
        public string ProviderId { get; set; }

        public int Rating { get; set; } // 1 to 5 stars
        public string? Comment { get; set; }
        public int? PunctualityRating { get; set; } // 1 to 5 stars
        public int? WorkQualityRating { get; set; } // 1 to 5 stars
        public int? CleanlinesRating { get; set; } // 1 to 5 stars
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public Booking Booking { get; set; }
        public Customer Customer { get; set; }
        public Provider Provider { get; set; }

    }
}