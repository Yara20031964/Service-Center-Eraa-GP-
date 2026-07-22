using KHDMA.Domain.Enums;

namespace KHDMA.Domain.Entities
{
    /// <summary>
    /// One row per booking status transition. This is what turns
    /// GET /api/booking/history into a real audit trail rather than a
    /// snapshot of the current state.
    /// </summary>
    public class BookingStatusHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BookingId { get; set; }
        public BookingStatus FromStatus { get; set; }
        public BookingStatus ToStatus { get; set; }

        /// <summary>Null when the transition was made by the system (dispatch worker, timeout).</summary>
        public string? ChangedByUserId { get; set; }

        public string? Reason { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public Booking Booking { get; set; }
    }
}
