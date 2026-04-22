using MediatR;

namespace KHDMA.Application.Features.Bookings.Queries.ExportBookings
{
    public class ExportBookingsQuery : IRequest<byte[]>
    {
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Format { get; set; } = "csv"; // "csv" or "excel"
    }
}
