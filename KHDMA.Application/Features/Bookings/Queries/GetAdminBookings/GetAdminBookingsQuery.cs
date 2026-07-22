using MediatR;
using KHDMA.Application.DTOs.Booking;
using Domain.Common;

namespace KHDMA.Application.Features.Bookings.Queries.GetAdminBookings
{
    public class GetAdminBookingsQuery : IRequest<PagedResponse<BookingListDto>>
    {
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? CustomerId { get; set; }
        public string? ProviderId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
