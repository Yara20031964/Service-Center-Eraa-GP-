using KHDMA.Domain.Enums;

namespace KHDMA.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CustomerId { get; set; }
        public string ProviderId { get; set; }
        public Guid ServiceId { get; set; }
        public  BookingType BookingType { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public string? CancelReason { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public Customer Customer { get; set; }
        public Provider Provider { get; set; }
        public Service Service { get; set; }
        public Payment? Payment { get; set; }
        public Review? Review { get; set; }
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}