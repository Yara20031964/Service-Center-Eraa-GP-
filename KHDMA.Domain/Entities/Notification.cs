namespace KHDMA.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } // Could be CustomerId or ProviderId
        public Guid BookingId { get; set; }
        public string Type { get; set; } // e.g., "BookingUpdate", "NewMessage", "PaymentStatus"
        public string Title { get; set; }
        public string Body { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public DateTime? SentAt { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public ApplicationUser User { get; set; }
        public Booking? Booking { get; set; }
    }
}