using System.Security.Claims;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Application.RealTime;
using KHDMA.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KHDMA.API.Hubs
{
    /// <summary>
    /// Booking lifecycle, dispatch and live-location channel, mounted at <c>/hubs/booking</c>.
    /// </summary>
    /// <remarks>
    /// There is deliberately no client-callable method that changes a booking's
    /// status. The original version exposed <c>SendStatusUpdate</c>, which let any
    /// connected client broadcast a forged "Completed" or "Arrived" to any booking
    /// group. Status transitions now happen only server-side through
    /// IBookingNotifier, after a REST endpoint has authorised the caller.
    /// </remarks>
    [Authorize]
    public class BookingHub : Hub
    {
        private readonly IBookingAccessService _access;
        private readonly IPresenceStore _presence;
        private readonly ILogger<BookingHub> _logger;

        public BookingHub(
            IBookingAccessService access,
            IPresenceStore presence,
            ILogger<BookingHub> logger)
        {
            _access = access;
            _presence = presence;
            _logger = logger;
        }

        private string UserId =>
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException("Unauthenticated");

        public override async Task OnConnectedAsync()
        {
            var userId = UserId;

            // Every connection joins its own user group so the dispatch engine can
            // reach one specific provider without tracking connection ids itself.
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.User(userId));
            await _presence.SetOnlineAsync(userId);

            if (Context.User?.IsInRole(nameof(UserRole.Provider)) == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.OnlineProviders);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null) await _presence.SetOfflineAsync(userId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to one booking's events. Rejects anyone who is not that
        /// booking's customer or its assigned provider.
        /// </summary>
        public async Task JoinBookingGroup(string bookingId)
        {
            if (!Guid.TryParse(bookingId, out var id))
                throw new HubException("Invalid booking id");

            if (!await _access.IsParticipantAsync(id, UserId))
            {
                _logger.LogWarning(
                    "User {UserId} attempted to join booking group {BookingId} without membership",
                    UserId, id);
                throw new HubException("You are not a participant in this booking");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Booking(id));
        }

        public async Task LeaveBookingGroup(string bookingId)
        {
            if (!Guid.TryParse(bookingId, out var id))
                throw new HubException("Invalid booking id");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Booking(id));
        }
    }
}
