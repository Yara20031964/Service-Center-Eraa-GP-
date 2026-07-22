using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Booking
{
    public class BookingDetailDto
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; }
        public BookingType BookingType { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public BookingStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public string? CancelReason { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
