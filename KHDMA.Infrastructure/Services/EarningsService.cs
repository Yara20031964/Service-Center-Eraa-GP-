using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace KHDMA.Infrastructure.Services
{
    public class EarningsService : IEarningsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommissionService _commissionService;

        public EarningsService(IUnitOfWork unitOfWork, ICommissionService commissionService)
        {
            _unitOfWork = unitOfWork;
            _commissionService = commissionService;
        }

        public async Task RecordEarningsAsync(Guid bookingId)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();
            var providerRepository = _unitOfWork.Repository<Provider>();

            var booking = await bookingRepository.GetOneAsync(b => b.Id == bookingId);
            if (booking == null) throw new Exception("Booking not found");
            if (booking.Status != BookingStatus.Completed) return;

            var provider = await providerRepository.GetOneAsync(p => p.ApplicationUserId == booking.ProviderId);
            if (provider == null) throw new Exception("Provider not found");

            // Get commission rate
            var rateResponse = await _commissionService.GetCurrentRateAsync();
            decimal rate = rateResponse.Success ? rateResponse.Data.Rate : 0.15m; // fallback to 15%

            decimal commissionAmount = booking.TotalPrice * rate;
            decimal providerEarnings = booking.TotalPrice - commissionAmount;

            provider.TotalEarnings += providerEarnings;
            provider.Balance += providerEarnings;
            provider.NumberOfJobsDone++;

            await providerRepository.UpdateAsync(provider);
            // In a real app, we'd also record a 'Transaction' record here.
            
            await _unitOfWork.CommitAsync();
        }
    }
}
