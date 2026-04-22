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

            var bookings = await bookingRepository.GetAsync(
                expression: b => b.CustomerId == request.UserId,
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
                .Select(b => new BookingListDto
                {
                    Id = b.Id,
                    ServiceName = b.Service.NameEn,
                    ProviderName = b.Provider.ApplicationUser.FullName,
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
