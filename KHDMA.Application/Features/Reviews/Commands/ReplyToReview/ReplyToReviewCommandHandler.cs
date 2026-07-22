using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;

namespace KHDMA.Application.Features.Reviews.Commands.ReplyToReview
{
    public class ReplyToReviewCommandHandler : IRequestHandler<ReplyToReviewCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReplyToReviewCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(ReplyToReviewCommand request, CancellationToken cancellationToken)
        {
            var reviewRepository = _unitOfWork.Repository<Review>();

            var review = await reviewRepository.GetOneAsync(r => r.Id == request.ReviewId);
            if (review == null) throw new Exception("Review not found");
            if (review.ProviderId != request.ProviderId) throw new Exception("Unauthorized");

            if (!string.IsNullOrEmpty(review.ProviderReply))
            {
                throw new Exception("You have already replied to this review");
            }

            review.ProviderReply = request.Reply;
            review.ProviderReplyAt = DateTime.UtcNow;

            await reviewRepository.UpdateAsync(review);
            await _unitOfWork.CommitAsync();

            return true;
        }
    }
}
