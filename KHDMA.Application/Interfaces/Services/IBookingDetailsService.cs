using Domain.Common;
using KHDMA.Application.DTOs.Booking;

namespace KHDMA.Application.Interfaces.Services;

public interface IBookingDetailsService
{
    Task<ApiResponse<BookingDetailDto>> GetAsync(Guid bookingId, string userId);
}
