using MediatR;

namespace KHDMA.Application.Features.Favorites.Commands.ToggleFavorite
{
    public class ToggleFavoriteProviderCommand : IRequest<bool>
    {
        public string CustomerId { get; set; }
        public string ProviderId { get; set; }
    }
}
