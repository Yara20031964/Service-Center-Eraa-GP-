using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace KHDMA.Infrastructure.Services
{
    public class DispatchService : IDispatchService
    {
        public async Task DispatchAsync(Booking booking)
        {
            // Skeleton implementation: Logic for dispatching to providers
            // Yara 
            booking.Status = BookingStatus.Dispatching;
            await Task.CompletedTask;
        }
    }
}
