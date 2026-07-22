using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.RealTime;
using Microsoft.AspNetCore.SignalR;

namespace KHDMA.API.Hubs
{
    /// <summary>SignalR implementation of <see cref="IChatNotifier"/>.</summary>
    public class ChatNotifier : IChatNotifier
    {
        private readonly IHubContext<ChatHub> _hub;

        public ChatNotifier(IHubContext<ChatHub> hub) => _hub = hub;

        // Per-user, not per-booking-group: IsMine differs by viewer. Targeting the
        // user group rather than a connection id also reaches their other devices.
        public Task MessageReceivedAsync(string recipientUserId, ChatMessageDto message)
            => _hub.Clients.Group(HubGroups.User(recipientUserId))
                .SendAsync(HubEvents.ReceiveMessage, message);

        public Task MessageReadAsync(Guid bookingId, Guid messageId)
            => _hub.Clients.Group(HubGroups.Booking(bookingId))
                .SendAsync(HubEvents.MessageRead, new { bookingId, messageId });

        public Task ChatLockedAsync(Guid bookingId)
            => _hub.Clients.Group(HubGroups.Booking(bookingId))
                .SendAsync(HubEvents.ChatLocked, new { bookingId });

        public Task PresenceChangedAsync(Guid bookingId, string userId, bool isOnline)
            => _hub.Clients.Group(HubGroups.Booking(bookingId))
                .SendAsync(HubEvents.PresenceChanged, new { userId, isOnline });
    }
}
