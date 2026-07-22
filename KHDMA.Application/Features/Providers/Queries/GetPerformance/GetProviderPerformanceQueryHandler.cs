using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Application.DTOs.Admin;
using Domain.Common;
using System.Linq.Expressions;

namespace KHDMA.Application.Features.Providers.Queries.GetPerformance
{
    public class GetProviderPerformanceQueryHandler : IRequestHandler<GetProviderPerformanceQuery, ApiResponse<ProviderPerformanceDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetProviderPerformanceQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<ProviderPerformanceDto>> Handle(GetProviderPerformanceQuery request, CancellationToken cancellationToken)
        {
            var providerRepository = _unitOfWork.Repository<Provider>();
            var bookingRepository = _unitOfWork.Repository<Booking>();

            var provider = await providerRepository.GetOneAsync(
                p => p.ApplicationUserId == request.ProviderId,
                includes: new Expression<Func<Provider, object>>[] { p => p.ApplicationUser }
            );

            if (provider == null) return ApiResponse<ProviderPerformanceDto>.Fail("Provider not found");

            var bookings = await bookingRepository.GetAsync(b => b.ProviderId == request.ProviderId);
            
            int total = bookings.Count();
            int completed = bookings.Count(b => b.Status == BookingStatus.Completed);
            int cancelled = bookings.Count(b => b.Status == BookingStatus.Cancelled);

            var dto = new ProviderPerformanceDto
            {
                ProviderId = provider.ApplicationUserId,
                ProviderName = provider.ApplicationUser.FullName,
                AverageRating = provider.Rating,
                TotalBookings = total,
                CompletedBookings = completed,
                CancelledBookings = cancelled,
                CompletionRate = total > 0 ? (double)completed / total * 100 : 0,
                CancellationRate = total > 0 ? (double)cancelled / total * 100 : 0,
                TotalEarnings = provider.TotalEarnings,
                CurrentBalance = provider.Balance
            };

            return ApiResponse<ProviderPerformanceDto>.Ok(dto);
        }
    }
}
