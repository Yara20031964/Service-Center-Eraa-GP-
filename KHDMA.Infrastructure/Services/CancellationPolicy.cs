using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;

namespace KHDMA.Infrastructure.Services
{
    public class CancellationPolicy : ICancellationPolicy
    {
        public async Task<bool> Evaluate(Booking booking)
        {
            // Skeleton implementation: Rules for free cancellation
            // Nour enforces policy here
            // For now, allow all cancellations
            return await Task.FromResult(true);
        }
    }
}
