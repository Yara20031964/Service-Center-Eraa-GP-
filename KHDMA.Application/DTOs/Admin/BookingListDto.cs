using System;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Admin
{
    public class BookingListDto
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; }
        public string ProviderName { get; set; }
        public string ServiceName { get; set; }
        public BookingStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
