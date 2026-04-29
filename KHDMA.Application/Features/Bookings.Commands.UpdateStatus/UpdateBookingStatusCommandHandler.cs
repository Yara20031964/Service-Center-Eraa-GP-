using MediatR;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.Features.Bookings.Commands.UpdateStatus
{
    public class UpdateBookingStatusCommandHandler : IRequestHandler<UpdateBookingStatusCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<KHDMA.Infrastructure.Hubs.BookingHub> _hubContext;

        public UpdateBookingStatusCommandHandler(IUnitOfWork unitOfWork, Microsoft.AspNetCore.SignalR.IHubContext<KHDMA.Infrastructure.Hubs.BookingHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        public async Task<bool> Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();

            var booking = await bookingRepository.GetOneAsync(b => b.Id == request.BookingId);
            if (booking == null) throw new Exception("Booking not found");
            if (booking.ProviderId != request.ProviderId) throw new Exception("Unauthorized");

            booking.Status = request.NewStatus;

            await bookingRepository.UpdateAsync(booking);
            await _unitOfWork.CommitAsync();

            await _hubContext.Clients.Group(booking.Id.ToString())
                .SendAsync("ReceiveStatusUpdate", booking.Id.ToString(), request.NewStatus.ToString(), request.Eta);

            return true;
        }
    }
}
