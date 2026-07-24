using KHDMA.Domain.Enums;

namespace KHDMA.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BookingId { get; set; }

        /// <summary>What the customer is charged: ServiceFee + VatAmount.</summary>
        public decimal Amount { get; set; }

        /// <summary>The service price before VAT. Commission is taken off this, never off the VAT.</summary>
        public decimal ServiceFee { get; set; }

        /// <summary>VAT charged on top of ServiceFee. Rendered as the "VAT (10%)" line in the app.</summary>
        public decimal VatAmount { get; set; }

        public decimal CommissionAmount { get; set; }
        public decimal ProviderEarning { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public  string? TransactionReference { get; set; }
        public DateTime? PaidAt { get; set; }

        /// <summary>When an admin approved the provider payout for this payment; null = still pending.</summary>
        public DateTime? PayoutApprovedAt { get; set; }

        //Navigation properties
        public Booking Booking { get; set; }
    }
}