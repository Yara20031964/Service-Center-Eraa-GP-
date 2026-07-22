using Domain.Common;
using KHDMA.Application.DTOs.RealTime;

namespace KHDMA.Application.Interfaces
{
    /// <summary>Outcome of one dispatch round.</summary>
    public enum DispatchOutcome
    {
        /// <summary>Job cards were broadcast; the countdown is running.</summary>
        Broadcast,

        /// <summary>No eligible provider in range, but rounds remain - the worker will expand and retry.</summary>
        NoCandidatesThisRound,

        /// <summary>Every round is exhausted. The booking is now NoProviderFound.</summary>
        Exhausted,

        /// <summary>The booking was not in a dispatchable state (already accepted, cancelled, unpaid).</summary>
        NotDispatchable,
    }

    public record DispatchRoundResult(DispatchOutcome Outcome, int Round, double RadiusKm, int ProvidersNotified);

    /// <summary>
    /// The SRS 2.2 broadcast dispatcher: find nearby eligible providers, send them
    /// the job card, and let the first to accept win.
    /// </summary>
    public interface IDispatchService
    {
        /// <summary>Runs the next dispatch round for a booking.</summary>
        Task<DispatchRoundResult> DispatchAsync(Guid bookingId, CancellationToken ct = default);

        /// <summary>
        /// Offers a booking to one specific provider only - the direct-booking path,
        /// where the customer chose the provider from their profile page.
        /// </summary>
        /// <remarks>
        /// Still goes through the same accept race and the same timeout, so a
        /// direct booking a provider ignores fails cleanly instead of hanging.
        /// It does not fall back to a broadcast: the customer asked for this
        /// person, and silently substituting someone else would be a surprise.
        /// </remarks>
        Task<DispatchRoundResult> DispatchToProviderAsync(Guid bookingId, string providerId, CancellationToken ct = default);

        /// <summary>
        /// Attempts to claim a dispatched booking for a provider.
        /// Exactly one concurrent caller can succeed; the rest get 409.
        /// </summary>
        Task<ApiResponse<AcceptResultDto>> AcceptAsync(Guid bookingId, string providerId, CancellationToken ct = default);

        /// <summary>
        /// Sweeps bookings whose round deadline has passed - expand the radius or
        /// give up. Called by DispatchTimeoutWorker.
        /// </summary>
        Task<int> ProcessExpiredRoundsAsync(CancellationToken ct = default);
    }
}
