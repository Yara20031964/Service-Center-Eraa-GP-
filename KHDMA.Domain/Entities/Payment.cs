using KHDMA.Domain.Enums;

namespace KHDMA.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BookingId { get; set; }
        public decimal Amount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal ProviderEarning { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public  string? TransactionReference { get; set; }
        public DateTime? PaidAt { get; set; }

        //Navigation properties
        public Booking Booking { get; set; }
    }
}