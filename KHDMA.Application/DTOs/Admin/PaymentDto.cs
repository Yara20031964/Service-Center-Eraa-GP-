using System;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Admin
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public decimal Amount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal ProviderEarning { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? TransactionReference { get; set; }
        public DateTime? PaidAt { get; set; }
        public string ProviderName { get; set; } 
        public string CustomerName { get; set; }
    }
}
