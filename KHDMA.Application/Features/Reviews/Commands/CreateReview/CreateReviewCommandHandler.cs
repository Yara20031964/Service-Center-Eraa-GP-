using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Application.Features.Reviews.Commands.CreateReview
{
    public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateReviewCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();
            var reviewRepository = _unitOfWork.Repository<Review>();
            var providerRepository = _unitOfWork.Repository<Provider>();

            var booking = await bookingRepository.GetOneAsync(b => b.Id == request.BookingId);
            if (booking == null) throw new Exception("Booking not found");
            if (booking.Status != BookingStatus.Completed) throw new Exception("Can only review completed bookings");
            if (booking.CustomerId != request.CustomerId) throw new Exception("Unauthorized to review this booking");

            var existingReview = await reviewRepository.GetOneAsync(r => r.BookingId == request.BookingId);
            if (existingReview != null) throw new Exception("Review already exists for this booking");

            var review = new Review
            {
                BookingId = request.BookingId,
                CustomerId = request.CustomerId,
                ProviderId = booking.ProviderId,
                Rating = request.Rating,
                Comment = request.Comment,
                PunctualityRating = request.PunctualityRating,
                WorkQualityRating = request.WorkQualityRating,
                CleanlinesRating = request.CleanlinesRating,
                CreateAt = DateTime.UtcNow
            };

            await reviewRepository.CreateAsync(review);

            // Update Provider overall rating
            var provider = await providerRepository.GetOneAsync(p => p.ApplicationUserId == booking.ProviderId);
            if (provider != null)
            {
                // Calculating weighted average: (CurrentRating * ReviewCount + NewRating) / (ReviewCount + 1)
                provider.Rating = (provider.Rating * provider.ReviewCount + request.Rating) / (provider.ReviewCount + 1);
                provider.ReviewCount++;
                await providerRepository.UpdateAsync(provider);
            }

            await _unitOfWork.CommitAsync();

            return review.Id;
        }
    }
}
