using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.Features.Bookings.Commands.RejectBooking
{
    public class RejectBookingCommandHandler : IRequestHandler<RejectBookingCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RejectBookingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(RejectBookingCommand request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();

            var booking = await bookingRepository.GetOneAsync(b => b.Id == request.BookingId);
            if (booking == null) throw new Exception("Booking not found");
            
            // If it was assigned to this provider, we might want to un-assign or mark as rejected by them
            // For now, let's just assume we record it or allow the booking to be picked by others
            if (booking.ProviderId == request.ProviderId && booking.Status == BookingStatus.Dispatching)
            {
                // In a real system, you'd probably have a BookingStatusHistory or a list of RejectedProviderIds
                // For simplified logic:
                // booking.ProviderId = null; // So other providers can see it
                // await bookingRepository.UpdateAsync(booking);
                // await _unitOfWork.CommitAsync();
            }

            return true;
        }
    }
}
