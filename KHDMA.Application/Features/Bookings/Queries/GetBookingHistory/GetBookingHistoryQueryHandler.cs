using MediatR;
using KHDMA.Application.DTOs.Booking;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using System.Linq.Expressions;
using Domain.Common;

namespace KHDMA.Application.Features.Bookings.Queries.GetBookingHistory
{
    public class GetBookingHistoryQueryHandler : IRequestHandler<GetBookingHistoryQuery, PagedResponse<BookingListDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetBookingHistoryQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<BookingListDto>> Handle(GetBookingHistoryQuery request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();
            
            var includes = new Expression<Func<Booking, object>>[]
            {
                b => b.Service,
                b => b.Provider,
                b => b.Provider.ApplicationUser
            };

            // Serves both sides of a booking: a customer sees the ones they raised,
            // a provider the ones they were assigned. Before this the filter was
            // CustomerId only, so providers got an empty list from their own history.
            var bookings = await bookingRepository.GetAsync(
                expression: b => b.CustomerId == request.UserId || b.ProviderId == request.UserId,
                includes: includes
            );

            var query = bookings.AsQueryable();

            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<KHDMA.Domain.Enums.BookingStatus>(request.Status, out var status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (request.FromDate.HasValue)
                query = query.Where(b => b.CreateAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(b => b.CreateAt <= request.ToDate.Value);

            var totalCount = query.Count();

            var data = query
                .OrderByDescending(b => b.CreateAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                // The rows are already materialised, so drop out of the expression
                // tree here - it is what lets the null-conditional below compile.
                .AsEnumerable()
                .Select(b => new BookingListDto
                {
                    Id = b.Id,
                    ServiceName = b.Service.NameEn,
                    ServiceNameEn = b.Service.NameEn,
                    ServiceNameAr = b.Service.NameAr,
                    // Null until a provider accepts - a Pending or Dispatching booking
                    // has none, and an unguarded dereference here threw instead of
                    // returning null.
                    ProviderName = b.Provider?.ApplicationUser?.FullName,
                    Status = b.Status,
                    ScheduledTime = b.ScheduledTime,
                    TotalPrice = b.TotalPrice,
                    CreateAt = b.CreateAt
                })
                .ToList();

            return PagedResponse<BookingListDto>.Ok(data, totalCount, request.Page, request.PageSize);
        }
    }
}
