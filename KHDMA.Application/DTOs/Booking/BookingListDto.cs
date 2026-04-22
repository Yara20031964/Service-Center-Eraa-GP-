using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Booking
{
    public class BookingListDto
    {
        public Guid Id { get; set; }
        public string ServiceName { get; set; }
        public string ProviderName { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
