namespace KHDMA.Application.Interfaces.Services
{
    public interface IEarningsService
    {
        Task RecordEarningsAsync(Guid bookingId);
    }
}
