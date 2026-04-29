using MediatR;
using Application.DTOs.Admin;
using Domain.Common;

namespace KHDMA.Application.Features.Favorites.Queries.GetFavoriteProviders
{
    public class GetFavoriteProvidersQuery : IRequest<ApiResponse<List<ProviderDto>>>
    {
        public string CustomerId { get; set; }
    }
}
