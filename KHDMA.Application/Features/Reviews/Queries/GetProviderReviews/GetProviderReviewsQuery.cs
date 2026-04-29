using MediatR;
using KHDMA.Application.DTOs.Review;
using Domain.Common;

namespace KHDMA.Application.Features.Reviews.Queries.GetProviderReviews
{
    public class GetProviderReviewsQuery : IRequest<PagedResponse<ReviewDto>>
    {
        public string ProviderId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
