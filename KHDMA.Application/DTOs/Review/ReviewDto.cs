namespace KHDMA.Application.DTOs.Review
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public string CustomerName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? ProviderReply { get; set; }
        public DateTime? ProviderReplyAt { get; set; }
        public int? PunctualityRating { get; set; }
        public int? WorkQualityRating { get; set; }
        public int? CleanlinessRating { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
