using KHDMA.Application.DTOs.RealTime;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.Interfaces.RealTime
{
    /// <summary>
    /// Real-time notifications for booking lifecycle events.
    /// Implemented in the API layer over SignalR; the Application layer depends only on this
    /// abstraction so that handlers never reference a hub type directly.
    /// </summary>
    public interface IBookingNotifier
    {
        // ---- to the customer, on booking:{id} ----
        Task BookingStatusChangedAsync(Guid bookingId, BookingStatus status, string? eta = null, string? message = null);
        Task ProviderAssignedAsync(Guid bookingId, ProviderCardDto provider);
        Task ProviderLocationAsync(Guid bookingId, ProviderLocationDto location);
        Task NoProviderFoundAsync(Guid bookingId, int roundsTried, double lastRadiusKm, bool refunded);
        Task PaymentStatusChangedAsync(Guid bookingId, PaymentStatusEventDto payment);

        // ---- to individual providers, on user:{providerId} ----
        Task JobDispatchedAsync(string providerId, JobCardDto card);
        Task JobDispatchExpiredAsync(IEnumerable<string> providerIds, Guid bookingId);
        Task JobTakenAsync(IEnumerable<string> providerIds, Guid bookingId);
        Task JobCancelledAsync(string providerId, Guid bookingId, string? reason);
    }

    /// <summary>Real-time push for the chat feature.</summary>
    public interface IChatNotifier
    {
        /// <summary>
        /// Delivered per-recipient rather than to the booking group, because
        /// <c>ChatMessageDto.IsMine</c> differs by viewer and a group send
        /// serializes the payload exactly once.
        /// </summary>
        Task MessageReceivedAsync(string recipientUserId, ChatMessageDto message);

        Task MessageReadAsync(Guid bookingId, Guid messageId);
        Task ChatLockedAsync(Guid bookingId);
        Task PresenceChangedAsync(Guid bookingId, string userId, bool isOnline);
    }
}
