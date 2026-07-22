namespace KHDMA.Application.DTOs.Booking
{
    public class CancelBookingDto
    {
        public Guid BookingId { get; set; }
        public string Reason { get; set; }
    }
}
