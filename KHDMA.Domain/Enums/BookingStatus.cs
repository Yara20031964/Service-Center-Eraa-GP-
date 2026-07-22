namespace KHDMA.Domain.Enums
{
    /// <summary>
    /// Lifecycle of a booking.
    /// </summary>
    /// <remarks>
    /// APPEND ONLY. Existing rows store the integer value, so inserting a member
    /// in the middle silently reassigns the meaning of every historical booking.
    /// </remarks>
    public enum BookingStatus
    {
        Pending = 0,
        Dispatching = 1,
        Accepted = 2,
        EnRoute = 3,
        Arrived = 4,
        InProgress = 5,
        Completed = 6,
        Cancelled = 7,

        /// <summary>Dispatch exhausted every round without an acceptance.</summary>
        NoProviderFound = 8,

        /// <summary>Terminal error state (payment failure, unrecoverable dispatch fault).</summary>
        Failed = 9,
    }
}
