using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;

namespace KHDMA.Application.Features.Reviews.Commands.UpdateReview
{
    public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateReviewCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
        {
            var reviewRepository = _unitOfWork.Repository<Review>();
            var providerRepository = _unitOfWork.Repository<Provider>();

            var review = await reviewRepository.GetOneAsync(r => r.Id == request.ReviewId);
            if (review == null) throw new Exception("Review not found");
            if (review.CustomerId != request.CustomerId) throw new Exception("Unauthorized");

            // Enforce 48h limit
            if (DateTime.UtcNow > review.CreateAt.AddHours(48))
            {
                throw new Exception("Review cannot be edited after 48 hours");
            }

            // Update Provider rating if rating changed
            if (review.Rating != request.Rating)
            {
                var provider = await providerRepository.GetOneAsync(p => p.ApplicationUserId == review.ProviderId);
                if (provider != null && provider.ReviewCount > 0)
                {
                    // Reverse old rating and add new one
                    provider.Rating = (provider.Rating * provider.ReviewCount - review.Rating + request.Rating) / provider.ReviewCount;
                    await providerRepository.UpdateAsync(provider);
                }
            }

            review.Rating = request.Rating;
            review.Comment = request.Comment;
            review.PunctualityRating = request.PunctualityRating;
            review.WorkQualityRating = request.WorkQualityRating;
            review.CleanlinesRating = request.CleanlinesRating;

            await reviewRepository.UpdateAsync(review);
            await _unitOfWork.CommitAsync();

            return true;
        }
    }
}
