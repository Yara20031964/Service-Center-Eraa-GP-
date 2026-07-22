using Domain.Common;
using KHDMA.Application.DTOs.Provider;

namespace KHDMA.Application.Interfaces.Services
{
    public interface IEarningsService
    {
        /// <summary>
        /// Credits a provider's wallet once a booking completes.
        /// Idempotent - calling it twice for the same booking must not pay twice.
        /// </summary>
        Task RecordEarningsAsync(Guid bookingId);

        /// <summary><paramref name="period"/> is daily | weekly | monthly | all.</summary>
        Task<ApiResponse<EarningsDto>> GetEarningsAsync(string providerId, string period);

        Task<ApiResponse<WalletDto>> GetWalletAsync(string providerId);

        Task<ApiResponse<ProviderPayoutDto>> RequestPayoutAsync(string providerId, decimal amount);
    }
}
