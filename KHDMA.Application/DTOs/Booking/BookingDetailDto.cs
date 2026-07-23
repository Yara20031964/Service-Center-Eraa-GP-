using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Booking
{
    public class BookingDetailDto
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderPhone { get; set; }
        public double? ProviderRating { get; set; }
        public string? ProviderPhoto { get; set; }
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public BookingType BookingType { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusLabelEn { get; set; } = string.Empty;
        public string StatusLabelAr { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public string? CancelReason { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? EnRouteAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}
