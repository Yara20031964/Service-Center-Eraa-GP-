using MediatR;
using KHDMA.Application.DTOs.Review;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using Domain.Common;
using System.Linq.Expressions;

namespace KHDMA.Application.Features.Reviews.Queries.GetProviderReviews
{
    public class GetProviderReviewsQueryHandler : IRequestHandler<GetProviderReviewsQuery, PagedResponse<ReviewDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetProviderReviewsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<ReviewDto>> Handle(GetProviderReviewsQuery request, CancellationToken cancellationToken)
        {
            var reviewRepository = _unitOfWork.Repository<Review>();

            var includes = new Expression<Func<Review, object>>[]
            {
                r => r.Customer,
                r => r.Customer.ApplicationUser
            };

            var reviews = await reviewRepository.GetAsync(
                expression: r => r.ProviderId == request.ProviderId && !r.IsDeleted && !r.IsHidden,
                includes: includes
            );

            var query = reviews.AsQueryable();
            var totalCount = query.Count();

            var data = query
                .OrderByDescending(r => r.CreateAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    BookingId = r.BookingId,
                    CustomerName = r.Customer.ApplicationUser.FullName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ProviderReply = r.ProviderReply,
                    ProviderReplyAt = r.ProviderReplyAt,
                    PunctualityRating = r.PunctualityRating,
                    WorkQualityRating = r.WorkQualityRating,
                    CleanlinessRating = r.CleanlinesRating,
                    CreateAt = r.CreateAt
                })
                .ToList();

            return PagedResponse<ReviewDto>.Ok(data, totalCount, request.Page, request.PageSize);
        }
    }
}
