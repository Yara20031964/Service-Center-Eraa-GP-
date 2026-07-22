using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Application.Interfaces;
using System.Linq.Expressions;

namespace KHDMA.Application.Features.Bookings.Queries.ExportBookings
{
    public class ExportBookingsQueryHandler : IRequestHandler<ExportBookingsQuery, byte[]>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExportService _exportService;

        public ExportBookingsQueryHandler(IUnitOfWork unitOfWork, IExportService exportService)
        {
            _unitOfWork = unitOfWork;
            _exportService = exportService;
        }

        public async Task<byte[]> Handle(ExportBookingsQuery request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();

            var includes = new Expression<Func<Booking, object>>[]
            {
                b => b.Service,
                b => b.Provider,
                b => b.Provider.ApplicationUser,
                b => b.Customer,
                b => b.Customer.ApplicationUser
            };

            var bookings = await bookingRepository.GetAsync(includes: includes);
            var query = bookings.AsQueryable();

            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<KHDMA.Domain.Enums.BookingStatus>(request.Status, out var status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (request.FromDate.HasValue)
                query = query.Where(b => b.CreateAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(b => b.CreateAt <= request.ToDate.Value);

            var exportData = query
                .OrderByDescending(b => b.CreateAt)
                .Select(b => new
                {
                    b.Id,
                    Customer = b.Customer.ApplicationUser.FullName,
                    Provider = b.Provider.ApplicationUser.FullName,
                    Service = b.Service.NameEn,
                    b.Status,
                    b.TotalPrice,
                    b.CreateAt,
                    b.ScheduledTime
                })
                .ToList();

            if (request.Format.ToLower() == "excel")
            {
                return _exportService.ExportToExcel(exportData);
            }
            else
            {
                return _exportService.ExportToCsv(exportData);
            }
        }
    }
}
