using Domain.Common;
using MediatR;
using KHDMA.Application.Interfaces;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace KHDMA.Application.Features.Bookings.Commands.CancelBooking
{
    public class CancelBookingCommandHandler
        : IRequestHandler<CancelBookingCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICancellationPolicy _cancellationPolicy;
        private readonly IBookingNotifier _notifier;
        private readonly ILogger<CancelBookingCommandHandler> _logger;

        public CancelBookingCommandHandler(
            IUnitOfWork unitOfWork,
            ICancellationPolicy cancellationPolicy,
            IBookingNotifier notifier,
            ILogger<CancelBookingCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _cancellationPolicy = cancellationPolicy;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(
            CancelBookingCommand request, CancellationToken cancellationToken)
        {
            var bookingRepository = _unitOfWork.Repository<Booking>();
            var booking = await bookingRepository.GetOneAsync(b => b.Id == request.BookingId);
            if (booking == null)
                return ApiResponse<bool>.NotFound("Booking not found");

            var fee = 0m;

            if (!request.IsAdmin)
            {
                if (booking.CustomerId != request.UserId)
                    return ApiResponse<bool>.Forbidden("You cannot cancel this booking.");

                var decision = await _cancellationPolicy.Evaluate(booking);
                if (!decision.Allowed)
                    return ApiResponse<bool>.Fail(decision.Reason!);

                fee = decision.Fee;
            }

            // Captured before the write so the provider notification below can tell
            // who was driving to a job that no longer exists.
            var assignedProviderId = booking.ProviderId;

            booking.Status = BookingStatus.Cancelled;
            booking.CancelReason = request.Reason;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancellationFee = fee;

            bookingRepository.Update(booking);
            await _unitOfWork.CommitAsync();

            await NotifyAsync(booking, assignedProviderId, request.Reason);

            var message = fee > 0m
                ? $"Cancelled. A cancellation fee of {fee:0.##} applies."
                : "Cancelled successfully";

            return ApiResponse<bool>.Ok(true, message);
        }

        /// <summary>
        /// Push is best-effort: the cancellation is already committed, and a dead
        /// socket must not turn a successful cancellation into an error.
        /// </summary>
        private async Task NotifyAsync(Booking booking, string? providerId, string? reason)
        {
            try
            {
                // Without this the provider keeps driving to a job the customer
                // already called off - nothing used to call JobCancelledAsync.
                if (!string.IsNullOrEmpty(providerId))
                    await _notifier.JobCancelledAsync(providerId, booking.Id, reason);

                // Reaches the customer's own other devices, and anyone watching the
                // booking after an admin cancelled it on their behalf.
                await _notifier.BookingStatusChangedAsync(
                    booking.Id, BookingStatus.Cancelled, message: reason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "Cancellation of booking {BookingId} committed but notifications failed",
                    booking.Id);
            }
        }
    }
}
