using KHDMA.Domain.Entities;

namespace KHDMA.Application.Interfaces
{
    public interface ICancellationPolicy
    {
        Task<bool> Evaluate(Booking booking);
    }
}
