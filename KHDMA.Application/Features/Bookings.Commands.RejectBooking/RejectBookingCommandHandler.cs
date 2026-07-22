using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.RejectBooking
{
    /// <summary>
    /// A provider declines a dispatched job card.
    /// </summary>
    /// <remarks>
    /// Under the broadcast model this is a local, per-provider act: the booking is
    /// offered to several providers at once and has no <c>ProviderId</c> until one
    /// accepts, so declining changes no booking state. The card is simply dismissed
    /// on that provider's device and the round continues for everyone else.
    ///
    /// The decline is recorded in <c>BookingStatusHistory</c> so the audit trail and
    /// the admin dashboard can show who was offered a job and passed - previously
    /// this handler returned true and wrote nothing at all.
    ///
    /// Note it deliberately does NOT end the round. Letting one provider's decline
    /// cancel a broadcast would hand any single provider the ability to starve the
    /// customer of the others.
    /// </remarks>
    public class RejectBookingCommandHandler : IRequestHandler<RejectBookingCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RejectBookingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(RejectBookingCommand request, CancellationToken cancellationToken)
        {
            var bookings = _unitOfWork.Repository<Booking>();
            var history = _unitOfWork.Repository<BookingStatusHistory>();

            var booking = await bookings.GetOneAsync(b => b.Id == request.BookingId);
            if (booking is null) throw new Exception("Booking not found");

            // Nothing to decline once someone has won the job.
            if (booking.Status != BookingStatus.Dispatching || booking.ProviderId is not null)
                return false;

            await history.CreateAsync(new BookingStatusHistory
            {
                BookingId = booking.Id,
                FromStatus = BookingStatus.Dispatching,
                ToStatus = BookingStatus.Dispatching,   // unchanged - this records who passed
                ChangedByUserId = request.ProviderId,
                Reason = "Provider declined the job card",
            });

            await _unitOfWork.CommitAsync();
            return true;
        }
    }
}
