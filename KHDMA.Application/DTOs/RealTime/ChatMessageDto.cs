namespace KHDMA.Application.DTOs.RealTime
{
    /// <summary>
    /// One chat bubble. Delivered by <c>HubEvents.ReceiveMessage</c> and by
    /// <c>GET /api/chat/{bookingId}/history</c>.
    /// </summary>
    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string? SenderName { get; set; }

        /// <summary>
        /// Computed server-side from the JWT so the client never compares ids.
        /// Maps onto the widget's existing isOutgoing parameter.
        /// </summary>
        public bool IsMine { get; set; }

        /// <summary>Text | Image | QuickReply.</summary>
        public string MessageType { get; set; } = "Text";

        /// <summary>
        /// For QuickReply this holds the i18n *key*, not prose, so the recipient
        /// renders it in their own language.
        /// </summary>
        public string? MessageText { get; set; }

        public string? AttachmentUrl { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }

    /// <summary>A row in the chat list.</summary>
    public class ChatThreadDto
    {
        public Guid BookingId { get; set; }
        public string PeerId { get; set; } = string.Empty;
        public string PeerName { get; set; } = string.Empty;
        public string? PeerRoleEn { get; set; }
        public string? PeerRoleAr { get; set; }
        public string? PeerAvatarUrl { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }

        /// <summary>True once the booking is Completed/Cancelled - disable the input bar.</summary>
        public bool IsLocked { get; set; }

        public string? ScheduleChipEn { get; set; }
        public string? ScheduleChipAr { get; set; }
    }

    /// <summary>Request body for <c>POST /api/chat/{bookingId}/messages</c>.</summary>
    public class SendMessageDto
    {
        public string MessageType { get; set; } = "Text";
        public string? MessageText { get; set; }
        public string? AttachmentUrl { get; set; }
    }
}
