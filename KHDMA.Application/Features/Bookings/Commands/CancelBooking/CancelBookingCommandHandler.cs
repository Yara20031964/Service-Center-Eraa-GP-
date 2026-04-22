using MediatR;
using KHDMA.Application.Interfaces;
using KHDMA.Domain.Enums;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;

namespace KHDMA.Application.Features.Bookings.Commands.CancelBooking
{
    public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICancellationPolicy _cancellationPolicy;

        public CancelBookingCommandHandler(IUnitOfWork unitOfWork, ICancellationPolicy cancellationPolicy)
        {
            _unitOfWork = unitOfWork;
            _cancellationPolicy = cancellationPolicy;
        }

        public async Task<bool> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();
            var booking = await bookingRepository.GetOneAsync(b => b.Id == request.BookingId);
            if (booking == null) return false;

            if (request.IsAdmin)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancelReason = request.Reason;
            }
            else
            {
                if (booking.CustomerId != request.UserId) throw new Exception("Unauthorized to cancel this booking.");

                var canCancel = await _cancellationPolicy.Evaluate(booking);
                if (!canCancel) throw new Exception("Cancellation policy violation: Cannot cancel at this time.");

                booking.Status = BookingStatus.Cancelled;
                booking.CancelReason = request.Reason;
            }

            bookingRepository.Update(booking);
            await _unitOfWork.CommitAsync();
            return true;
        }
    }
}
