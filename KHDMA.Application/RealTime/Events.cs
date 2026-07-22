namespace KHDMA.Application.RealTime
{
    /// <summary>
    /// Every server -> client SignalR event name, in one place.
    /// </summary>
    /// <remarks>
    /// These strings are a published contract with the Flutter client
    /// (docs/API_CONTRACTS.md section 3.4). Renaming one is a breaking change:
    /// the client silently stops receiving the event rather than failing loudly.
    /// </remarks>
    public static class HubEvents
    {
        // ---- server -> provider ----
        /// <summary>Payload: <see cref="DTOs.RealTime.JobCardDto"/>. Start the 60s countdown.</summary>
        public const string JobDispatched = "JobDispatched";

        /// <summary>Payload: <c>{ bookingId }</c>. The round elapsed with no accept - dismiss the card.</summary>
        public const string JobDispatchExpired = "JobDispatchExpired";

        /// <summary>Payload: <c>{ bookingId }</c>. Another provider won - dismiss the card.</summary>
        public const string JobTaken = "JobTaken";

        /// <summary>Payload: <c>{ bookingId, reason }</c>. Customer or admin cancelled.</summary>
        public const string JobCancelled = "JobCancelled";

        // ---- server -> customer ----
        /// <summary>Payload: <see cref="DTOs.RealTime.BookingStatusEventDto"/>. Drives the tracking stepper.</summary>
        public const string BookingStatusChanged = "BookingStatusChanged";

        /// <summary>Payload: <see cref="DTOs.RealTime.ProviderCardDto"/>. Show "Provider Found!".</summary>
        public const string ProviderAssigned = "ProviderAssigned";

        /// <summary>Payload: <see cref="DTOs.RealTime.ProviderLocationDto"/>. ~every 5s while En Route.</summary>
        public const string ProviderLocation = "ProviderLocation";

        /// <summary>Payload: <c>{ bookingId, roundsTried }</c>. All dispatch rounds exhausted.</summary>
        public const string NoProviderFound = "NoProviderFound";

        /// <summary>Payload: <see cref="DTOs.RealTime.PaymentStatusEventDto"/>.</summary>
        public const string PaymentStatusChanged = "PaymentStatusChanged";

        // ---- chat ----
        /// <summary>Payload: <see cref="DTOs.RealTime.ChatMessageDto"/>.</summary>
        public const string ReceiveMessage = "ReceiveMessage";

        /// <summary>Payload: <c>{ bookingId, messageId }</c>.</summary>
        public const string MessageRead = "MessageRead";

        /// <summary>Payload: <c>{ bookingId }</c>. Booking finished - disable the input bar.</summary>
        public const string ChatLocked = "ChatLocked";

        /// <summary>Payload: <c>{ userId, isOnline }</c>. Drives the chat_status_online label.</summary>
        public const string PresenceChanged = "PresenceChanged";
    }

    /// <summary>
    /// SignalR group naming. Always build group names through these helpers -
    /// a hand-written string that differs by one character routes messages nowhere,
    /// with no error.
    /// </summary>
    public static class HubGroups
    {
        /// <summary>The customer plus the assigned provider for one booking.</summary>
        public static string Booking(Guid bookingId) => $"booking:{bookingId}";

        /// <summary>All of one user's connections. Joined automatically on connect.</summary>
        public static string User(string userId) => $"user:{userId}";

        /// <summary>Every provider currently marked Online.</summary>
        public const string OnlineProviders = "providers:online";
    }
}
