using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using Application.DTOs.Admin;
using Domain.Common;
using System.Linq.Expressions;

namespace KHDMA.Application.Features.Favorites.Queries.GetFavoriteProviders
{
    public class GetFavoriteProvidersQueryHandler : IRequestHandler<GetFavoriteProvidersQuery, ApiResponse<List<ProviderDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetFavoriteProvidersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<List<ProviderDto>>> Handle(GetFavoriteProvidersQuery request, CancellationToken cancellationToken)
        {
            var favoriteRepository = _unitOfWork.Repository<CustomerFavoriteProvider>();

            var includes = new Expression<Func<CustomerFavoriteProvider, object>>[]
            {
                f => f.Provider,
                f => f.Provider.ApplicationUser
            };

            var favorites = await favoriteRepository.GetAsync(
                expression: f => f.CustomerId == request.CustomerId,
                includes: includes
            );

            var result = favorites.Select(f => new ProviderDto
            {
                Id = f.ProviderId,
                FullName = f.Provider.ApplicationUser.FullName,
                Email = f.Provider.ApplicationUser.Email ?? "",
                Phone = f.Provider.ApplicationUser.PhoneNumber,
                ProviderState = f.Provider.State,
                AvailabilityStatus = f.Provider.AvailabilityStatus,
                ServiceArea = f.Provider.ServiceArea,
                HourlyRate = f.Provider.HourlyRate ?? 0,
                Rating = f.Provider.Rating,
                ReviewCount = f.Provider.ReviewCount
            }).ToList();

            return ApiResponse<List<ProviderDto>>.Ok(result);
        }
    }
}
