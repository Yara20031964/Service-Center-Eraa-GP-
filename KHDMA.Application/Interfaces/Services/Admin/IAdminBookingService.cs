using System;
using System.Threading.Tasks;
using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.Interfaces.Services.Admin
{
    public interface IAdminBookingService
    {
        Task<PagedResponse<BookingListDto>> GetAllBookingsAsync(int pageNumber, int pageSize, BookingStatus? status, DateTime? fromDate, DateTime? toDate, string? customerId, string? providerId);
        Task<ApiResponse<BookingDetailDto>> GetBookingDetailsAsync(Guid bookingId);
        Task<ApiResponse<bool>> CancelBookingAsync(Guid bookingId, string reason);
        Task<ApiResponse<object>> GetBookingStatusHistoryAsync(Guid bookingId); // Returning object for now, or could define a specific DTO
    }
}
