using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Booking
{
    public class BookingListDto
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Kept for older clients. It carries the English name; a localized client
        /// should read <see cref="ServiceNameEn"/>/<see cref="ServiceNameAr"/>
        /// instead, which is why the list rendered in English regardless of locale.
        /// </summary>
        public string ServiceName { get; set; }
        public string ServiceNameEn { get; set; } = string.Empty;
        public string ServiceNameAr { get; set; } = string.Empty;

        /// <summary>Null until a provider accepts the booking.</summary>
        public string? ProviderName { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
