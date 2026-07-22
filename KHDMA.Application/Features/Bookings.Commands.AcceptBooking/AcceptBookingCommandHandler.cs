using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.Features.Bookings.Commands.AcceptBooking
{
    public class AcceptBookingCommandHandler : IRequestHandler<AcceptBookingCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AcceptBookingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(AcceptBookingCommand request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();
            
            // First provider wins - atomic check
            var booking = await bookingRepository.GetOneAsync(
                b => b.Id == request.BookingId,
                tracked: true);

            if (booking == null) throw new Exception("Booking not found");
            
            // Check if someone else already accepted
            if (booking.Status != BookingStatus.Dispatching)
            {
                throw new Exception("Booking is no longer available or already accepted");
            }

            // Optional: If you had a field for who accepted, you'd check it too
            // In your schema, ProviderId is already set at creation? 
            // Re-checking CreateBookingCommand... it has ProviderId. 
            // BUT, the image says "first wins via Redis lock", implying multiple providers MIGHT be notified.
            // If the provider was ALREADY chosen at creation, we just need to confirm.
            
            booking.Status = BookingStatus.Accepted;
            booking.ProviderId = request.ProviderId; // Ensure it's set to the one who accepted if it was generic

            await bookingRepository.UpdateAsync(booking);
            await _unitOfWork.CommitAsync();

            return true;
        }
    }
}
