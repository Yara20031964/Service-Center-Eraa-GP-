using System.Security.Claims;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Application.RealTime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KHDMA.API.Hubs
{
    /// <summary>
    /// In-booking chat, mounted at <c>/hubs/chat</c>.
    /// </summary>
    /// <remarks>
    /// The hub is a thin transport over <see cref="IChatService"/>. All the rules -
    /// membership, the PII filter, the completed-booking lock, persistence - live
    /// in the service, so sending over the hub and sending over
    /// <c>POST /api/chat/{id}/messages</c> behave identically. A client cannot pick
    /// the transport that skips a check.
    /// </remarks>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chat;
        private readonly IBookingAccessService _access;
        private readonly IPresenceStore _presence;

        public ChatHub(IChatService chat, IBookingAccessService access, IPresenceStore presence)
        {
            _chat = chat;
            _access = access;
            _presence = presence;
        }

        private string UserId =>
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException("Unauthenticated");

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.User(UserId));
            await _presence.SetOnlineAsync(UserId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null) await _presence.SetOfflineAsync(userId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>Subscribe to one conversation. Same membership rule as BookingHub.</summary>
        public async Task JoinBooking(string bookingId)
        {
            if (!Guid.TryParse(bookingId, out var id))
                throw new HubException("Invalid booking id");

            if (!await _access.IsParticipantAsync(id, UserId))
                throw new HubException("You are not a participant in this booking");

            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Booking(id));

            // Tell the peer we are here so their online indicator updates.
            await Clients.OthersInGroup(HubGroups.Booking(id))
                .SendAsync(HubEvents.PresenceChanged, new { userId = UserId, isOnline = true });
        }

        public async Task LeaveBooking(string bookingId)
        {
            if (!Guid.TryParse(bookingId, out var id))
                throw new HubException("Invalid booking id");

            await Clients.OthersInGroup(HubGroups.Booking(id))
                .SendAsync(HubEvents.PresenceChanged, new { userId = UserId, isOnline = false });

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Booking(id));
        }

        /// <summary>
        /// Send a message. Throws <see cref="HubException"/> on rejection so the
        /// client's invoke() promise rejects with the reason - a PII block or a
        /// locked conversation must surface in the input bar, not fail silently.
        /// </summary>
        public async Task<ChatMessageDto> SendMessage(string bookingId, SendMessageDto dto)
        {
            if (!Guid.TryParse(bookingId, out var id))
                throw new HubException("Invalid booking id");

            var result = await _chat.SendMessageAsync(id, UserId, dto);
            if (!result.Success || result.Data is null)
                throw new HubException(result.Message);

            return result.Data;
        }

        public async Task MarkRead(string bookingId)
        {
            if (!Guid.TryParse(bookingId, out var id))
                throw new HubException("Invalid booking id");

            await _chat.MarkReadAsync(id, UserId);
        }
    }
}
