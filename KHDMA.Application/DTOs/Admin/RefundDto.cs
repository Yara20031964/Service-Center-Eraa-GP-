using System;

namespace KHDMA.Application.DTOs.Admin
{
    public class RefundDto
    {
        public Guid PaymentId { get; set; }
        public string Reason { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
