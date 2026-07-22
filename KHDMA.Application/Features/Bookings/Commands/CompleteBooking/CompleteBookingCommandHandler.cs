using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.CompleteBooking
{
    public class CompleteBookingCommandHandler : IRequestHandler<CompleteBookingCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEarningsService _earningsService;
        private readonly IBookingNotifier _notifier;
        private readonly IChatNotifier _chatNotifier;

        public CompleteBookingCommandHandler(
            IUnitOfWork unitOfWork,
            IEarningsService earningsService,
            IBookingNotifier notifier,
            IChatNotifier chatNotifier)
        {
            _unitOfWork = unitOfWork;
            _earningsService = earningsService;
            _notifier = notifier;
            _chatNotifier = chatNotifier;
        }

        public async Task<bool> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
        {
            var bookings = _unitOfWork.Repository<Booking>();
            var history = _unitOfWork.Repository<BookingStatusHistory>();

            var booking = await bookings.GetOneAsync(b => b.Id == request.BookingId);
            if (booking is null) throw new Exception("Booking not found");
            if (booking.ProviderId != request.ProviderId) throw new Exception("Unauthorized");

            // Idempotent: a retried request must not credit the provider twice.
            if (booking.Status == BookingStatus.Completed) return true;

            if (booking.Status != BookingStatus.InProgress)
                throw new Exception($"Cannot complete a booking that is {booking.Status}");

            var previous = booking.Status;
            booking.Status = BookingStatus.Completed;
            booking.CompletedAt = DateTime.UtcNow;

            bookings.Update(booking);

            await history.CreateAsync(new BookingStatusHistory
            {
                BookingId = booking.Id,
                FromStatus = previous,
                ToStatus = BookingStatus.Completed,
                ChangedByUserId = request.ProviderId,
            });

            await _unitOfWork.CommitAsync();

            await _earningsService.RecordEarningsAsync(booking.Id);

            await _notifier.BookingStatusChangedAsync(booking.Id, BookingStatus.Completed);

            // The conversation closes with the job (SRS 7.3).
            await _chatNotifier.ChatLockedAsync(booking.Id);

            return true;
        }
    }
}
