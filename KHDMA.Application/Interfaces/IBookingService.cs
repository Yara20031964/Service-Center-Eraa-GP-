using KHDMA.Application.DTOs.Booking;
using KHDMA.Application.DTOs.Responses;

namespace KHDMA.Application.Interfaces
{
    public interface IBookingService
    {
        Task<Guid> CreateBookingAsync(CreateBookingDto dto, string customerId);
        Task<bool> CancelBookingAsync(Guid bookingId, string reason, string userId, bool isAdmin = false);
        Task<BookingDetailDto> GetBookingDetailsAsync(Guid bookingId);
        Task<IEnumerable<BookingListDto>> GetBookingHistoryAsync(string userId, string? status = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<byte[]> ExportBookingsAsync(string? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    }
}
