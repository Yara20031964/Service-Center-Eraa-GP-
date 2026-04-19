using System;

namespace KHDMA.Application.DTOs.Admin
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public string CustomerName { get; set; }
        public string ProviderName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int? PunctualityRating { get; set; }
        public int? WorkQualityRating { get; set; }
        public int? CleanlinesRating { get; set; }
        public DateTime CreateAt { get; set; }
        public bool IsHidden { get; set; }
    }
}
