using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.UpdateStatus
{
    public class UpdateBookingStatusCommandHandler : IRequestHandler<UpdateBookingStatusCommand, bool>
    {
        /// <summary>
        /// The only transitions a provider may drive, and what each may follow.
        /// </summary>
        /// <remarks>
        /// Without this a provider could mark a booking Arrived before accepting it,
        /// or jump from Accepted straight to Completed and skip the job - and the
        /// customer's tracking stepper would render states the work never passed
        /// through.
        /// </remarks>
        private static readonly Dictionary<BookingStatus, BookingStatus[]> AllowedFrom = new()
        {
            [BookingStatus.EnRoute]    = [BookingStatus.Accepted],
            [BookingStatus.Arrived]    = [BookingStatus.EnRoute],
            [BookingStatus.InProgress] = [BookingStatus.Arrived],
            [BookingStatus.Completed]  = [BookingStatus.InProgress],
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingNotifier _notifier;

        public UpdateBookingStatusCommandHandler(IUnitOfWork unitOfWork, IBookingNotifier notifier)
        {
            _unitOfWork = unitOfWork;
            _notifier = notifier;
        }

        public async Task<bool> Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
        {
            var bookings = _unitOfWork.Repository<Booking>();
            var history = _unitOfWork.Repository<BookingStatusHistory>();

            var booking = await bookings.GetOneAsync(b => b.Id == request.BookingId);
            if (booking is null) throw new Exception("Booking not found");
            if (booking.ProviderId != request.ProviderId) throw new Exception("Unauthorized");

            if (booking.Status == request.NewStatus) return true;   // idempotent retry

            if (!AllowedFrom.TryGetValue(request.NewStatus, out var allowed))
                throw new Exception($"{request.NewStatus} cannot be set through this endpoint");

            if (!allowed.Contains(booking.Status))
                throw new Exception($"Cannot move a booking from {booking.Status} to {request.NewStatus}");

            var previous = booking.Status;
            booking.Status = request.NewStatus;

            var now = DateTime.UtcNow;
            switch (request.NewStatus)
            {
                case BookingStatus.EnRoute: booking.EnRouteAt = now; break;
                case BookingStatus.Arrived: booking.ArrivedAt = now; break;
                case BookingStatus.InProgress: booking.StartedAt = now; break;
                case BookingStatus.Completed: booking.CompletedAt = now; break;
            }

            bookings.Update(booking);

            await history.CreateAsync(new BookingStatusHistory
            {
                BookingId = booking.Id,
                FromStatus = previous,
                ToStatus = request.NewStatus,
                ChangedByUserId = request.ProviderId,
            });

            await _unitOfWork.CommitAsync();

            await _notifier.BookingStatusChangedAsync(booking.Id, request.NewStatus, request.Eta);

            return true;
        }
    }
}
