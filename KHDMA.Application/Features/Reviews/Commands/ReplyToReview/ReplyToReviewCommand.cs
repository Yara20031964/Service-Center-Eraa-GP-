using MediatR;

namespace KHDMA.Application.Features.Reviews.Commands.ReplyToReview
{
    public class ReplyToReviewCommand : IRequest<bool>
    {
        public Guid ReviewId { get; set; }
        public string ProviderId { get; set; }
        public string Reply { get; set; }
    }
}
