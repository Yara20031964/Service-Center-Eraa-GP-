using MediatR;
using KHDMA.Application.DTOs.Booking;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using System.Linq.Expressions;
using Domain.Common;

namespace KHDMA.Application.Features.Bookings.Queries.GetAdminBookings
{
    public class GetAdminBookingsQueryHandler : IRequestHandler<GetAdminBookingsQuery, PagedResponse<BookingListDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAdminBookingsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<BookingListDto>> Handle(GetAdminBookingsQuery request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();

            var includes = new Expression<Func<Booking, object>>[]
            {
                b => b.Service,
                b => b.Provider,
                b => b.Provider.ApplicationUser
            };

            var bookings = await bookingRepository.GetAsync(includes: includes);
            var query = bookings.AsQueryable();

            // Filtering
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<KHDMA.Domain.Enums.BookingStatus>(request.Status, out var status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (request.FromDate.HasValue)
                query = query.Where(b => b.CreateAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(b => b.CreateAt <= request.ToDate.Value);

            if (!string.IsNullOrEmpty(request.CustomerId))
                query = query.Where(b => b.CustomerId == request.CustomerId);

            if (!string.IsNullOrEmpty(request.ProviderId))
                query = query.Where(b => b.ProviderId == request.ProviderId);

            var totalCount = query.Count();

            var data = query
                .OrderByDescending(b => b.CreateAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                // Already materialised by GetAsync above, but be explicit: the
                // projection runs in memory, so the null-conditional below is what
                // keeps a provider-less booking (Pending/Dispatching) from throwing.
                .AsEnumerable()
                .Select(b => new BookingListDto
                {
                    Id = b.Id,
                    ServiceName = b.Service.NameEn,
                    // Null until a provider accepts - an admin list includes bookings
                    // still being dispatched, which have no provider yet.
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
