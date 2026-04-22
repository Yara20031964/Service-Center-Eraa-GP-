using MediatR;
using KHDMA.Application.DTOs.Booking;
using Domain.Common;

namespace KHDMA.Application.Features.Bookings.Queries.GetBookingHistory
{
    public class GetBookingHistoryQuery : IRequest<PagedResponse<BookingListDto>>
    {
        public string UserId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
