using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;

namespace KHDMA.Application.Features.Favorites.Commands.ToggleFavorite
{
    public class ToggleFavoriteProviderCommandHandler : IRequestHandler<ToggleFavoriteProviderCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ToggleFavoriteProviderCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(ToggleFavoriteProviderCommand request, CancellationToken cancellationToken)
        {
            var favoriteRepository = _unitOfWork.Repository<CustomerFavoriteProvider>();
            
            var favorite = await favoriteRepository.GetOneAsync(f => f.CustomerId == request.CustomerId && f.ProviderId == request.ProviderId);

            if (favorite != null)
            {
                favoriteRepository.Delete(favorite);
                await _unitOfWork.CommitAsync();
                return false; // Removed
            }
            else
            {
                var newFavorite = new CustomerFavoriteProvider
                {
                    CustomerId = request.CustomerId,
                    ProviderId = request.ProviderId,
                    CreatedAt = DateTime.UtcNow
                };
                await favoriteRepository.CreateAsync(newFavorite);
                await _unitOfWork.CommitAsync();
                return true; // Added
            }
        }
    }
}
