using KHDMA.Application.Common;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.RealTime;
using KHDMA.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace KHDMA.API.Hubs
{
    /// <summary>
    /// SignalR implementation of <see cref="IBookingNotifier"/>.
    /// Keeps the hub type out of the Application layer.
    /// </summary>
    public class BookingNotifier : IBookingNotifier
    {
        private readonly IHubContext<BookingHub> _hub;

        public BookingNotifier(IHubContext<BookingHub> hub) => _hub = hub;

        // ---- to the customer, on booking:{id} ----

        public Task BookingStatusChangedAsync(Guid bookingId, BookingStatus status, string? eta = null, string? message = null)
        {
            // ETA arrives as free text from the provider ("20 min"); parse what we
            // can and leave it null otherwise rather than shipping a bad number.
            int? etaMinutes = int.TryParse(eta, out var m) ? m : null;

            var payload = new BookingStatusEventDto
            {
                BookingId = bookingId,
                Status = status.ToString(),
                StatusLabelEn = BookingStatusLabels.En(status),
                StatusLabelAr = BookingStatusLabels.Ar(status),
                ChangedAt = DateTime.UtcNow,
                EtaMinutes = etaMinutes,
                Message = message,
            };

            return _hub.Clients.Group(HubGroups.Booking(bookingId))
                .SendAsync(HubEvents.BookingStatusChanged, payload);
        }

        public Task ProviderAssignedAsync(Guid bookingId, ProviderCardDto provider)
            => _hub.Clients.Group(HubGroups.Booking(bookingId))
                .SendAsync(HubEvents.ProviderAssigned, provider);

        public Task ProviderLocationAsync(Guid bookingId, ProviderLocationDto location)
            => _hub.Clients.Group(HubGroups.Booking(bookingId))
                .SendAsync(HubEvents.ProviderLocation, location);

        public Task NoProviderFoundAsync(Guid bookingId, int roundsTried, double lastRadiusKm, bool refunded)
            => _hub.Clients.Group(HubGroups.Booking(bookingId))
                .SendAsync(HubEvents.NoProviderFound, new NoProviderFoundEventDto
                {
                    BookingId = bookingId,
                    RoundsTried = roundsTried,
                    LastRadiusKm = lastRadiusKm,
                    Refunded = refunded,
                });

        public Task PaymentStatusChangedAsync(Guid bookingId, PaymentStatusEventDto payment)
            => _hub.Clients.Group(HubGroups.Booking(bookingId))
                .SendAsync(HubEvents.PaymentStatusChanged, payment);

        // ---- to individual providers, on user:{providerId} ----

        public Task JobDispatchedAsync(string providerId, JobCardDto card)
            => _hub.Clients.Group(HubGroups.User(providerId))
                .SendAsync(HubEvents.JobDispatched, card);

        public Task JobDispatchExpiredAsync(IEnumerable<string> providerIds, Guid bookingId)
            => FanOutAsync(providerIds, HubEvents.JobDispatchExpired,
                new JobDismissedEventDto { BookingId = bookingId, Reason = "expired" });

        public Task JobTakenAsync(IEnumerable<string> providerIds, Guid bookingId)
            => FanOutAsync(providerIds, HubEvents.JobTaken,
                new JobDismissedEventDto { BookingId = bookingId, Reason = "taken" });

        public Task JobCancelledAsync(string providerId, Guid bookingId, string? reason)
            => _hub.Clients.Group(HubGroups.User(providerId))
                .SendAsync(HubEvents.JobCancelled,
                    new JobDismissedEventDto { BookingId = bookingId, Reason = reason });

        /// <summary>
        /// Sends to many user groups at once. Clients.Groups takes the list in a
        /// single call, so this is one backplane publish rather than N.
        /// </summary>
        private Task FanOutAsync(IEnumerable<string> providerIds, string eventName, object payload)
        {
            var groups = providerIds.Select(HubGroups.User).ToList();
            if (groups.Count == 0) return Task.CompletedTask;
            return _hub.Clients.Groups(groups).SendAsync(eventName, payload);
        }
    }
}
