namespace KHDMA.Application.DTOs.Review
{
    public class ReviewDto
    {
        public Guid BookingId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int? PunctualityRating { get; set; }
        public int? WorkQualityRating { get; set; }
        public int? CleanlinessRating { get; set; }
    }
}
