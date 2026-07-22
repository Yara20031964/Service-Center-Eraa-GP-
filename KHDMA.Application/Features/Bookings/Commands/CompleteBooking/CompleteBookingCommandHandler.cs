using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Enums;
using KHDMA.Domain.Entities;

namespace KHDMA.Application.Features.Bookings.Commands.CompleteBooking
{
    public class CompleteBookingCommandHandler : IRequestHandler<CompleteBookingCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEarningsService _earningsService;

        public CompleteBookingCommandHandler(IUnitOfWork unitOfWork, IEarningsService earningsService)
        {
            _unitOfWork = unitOfWork;
            _earningsService = earningsService;
        }

        public async Task<bool> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();

            var booking = await bookingRepository.GetOneAsync(b => b.Id == request.BookingId);
            if (booking == null) throw new Exception("Booking not found");
            if (booking.ProviderId != request.ProviderId) throw new Exception("Unauthorized");
            
            // Should be InProgress or maybe other states depending on workflow
            if (booking.Status == BookingStatus.Completed) return true;

            booking.Status = BookingStatus.Completed;
            await bookingRepository.UpdateAsync(booking);
            await _unitOfWork.CommitAsync();

            await _earningsService.RecordEarningsAsync(booking.Id);

            return true;
        }
    }
}
