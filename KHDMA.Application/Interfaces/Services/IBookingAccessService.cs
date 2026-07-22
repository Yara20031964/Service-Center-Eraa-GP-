namespace KHDMA.Application.Interfaces.Services
{
    /// <summary>Who is allowed to see or act on a booking.</summary>
    public record BookingParticipants(Guid BookingId, string CustomerId, string? ProviderId, bool IsClosed);

    /// <summary>
    /// Membership checks for booking-scoped real-time channels.
    /// </summary>
    /// <remarks>
    /// Both hubs and the chat service go through this. Without it, any
    /// authenticated client could join <c>booking:{anyGuid}</c> and eavesdrop on
    /// another customer's location stream and chat.
    /// </remarks>
    public interface IBookingAccessService
    {
        Task<BookingParticipants?> GetParticipantsAsync(Guid bookingId);

        /// <summary>True when the user is the booking's customer or its assigned provider.</summary>
        Task<bool> IsParticipantAsync(Guid bookingId, string userId);
    }
}
