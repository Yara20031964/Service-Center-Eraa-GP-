using MediatR;

namespace KHDMA.Application.Features.Reviews.Commands.CreateReview
{
    public class CreateReviewCommand : IRequest<Guid>
    {
        public Guid BookingId { get; set; }
        public string CustomerId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int? PunctualityRating { get; set; }
        public int? WorkQualityRating { get; set; }
        public int? CleanlinesRating { get; set; }
    }
}
