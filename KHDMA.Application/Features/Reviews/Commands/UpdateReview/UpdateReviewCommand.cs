using MediatR;

namespace KHDMA.Application.Features.Reviews.Commands.UpdateReview
{
    public class UpdateReviewCommand : IRequest<bool>
    {
        public Guid ReviewId { get; set; }
        public string CustomerId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int? PunctualityRating { get; set; }
        public int? WorkQualityRating { get; set; }
        public int? CleanlinesRating { get; set; }
    }
}
