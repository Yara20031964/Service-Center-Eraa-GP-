using KHDMA.Domain.Entities;

namespace KHDMA.Application.Interfaces
{
    public interface IDispatchService
    {
        Task DispatchAsync(Booking booking);
    }
}
