using System;
using System.Collections.Generic;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Admin
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
        public BookingStatus Status { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? Address { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public string? CancelReason { get; set; } // useful for admin panel if a booking was cancelled
        public DateTime CreateAt { get; set; }
        
        public PaymentDto? PaymentDetails { get; set; }
        public ReviewDto? ReviewDetails { get; set; }
    }
}
