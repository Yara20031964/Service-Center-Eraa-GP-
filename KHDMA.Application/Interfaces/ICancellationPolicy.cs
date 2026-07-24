using KHDMA.Domain.Entities;

namespace KHDMA.Application.Interfaces
{
    /// <summary>
    /// What the policy decided about one cancellation request.
    /// </summary>
    /// <remarks>
    /// This replaces a bare bool. The bool was ambiguous - the policy computed
    /// "may cancel <i>free of charge</i>" but the caller read it as "may cancel
    /// at all", which both blocked late cancellations outright and left
    /// <see cref="CancellationPolicy.CancellationFee"/> unreachable.
    /// </remarks>
    /// <param name="Allowed">
    /// False only when there is genuinely nothing left to cancel - a booking that
    /// already completed or was already cancelled. Being late is not a refusal;
    /// it is a fee.
    /// </param>
    /// <param name="Fee">What the customer owes. Zero inside the free window.</param>
    /// <param name="Reason">Why a refused cancellation was refused. Null when allowed.</param>
    public sealed record CancellationDecision(bool Allowed, decimal Fee, string? Reason)
    {
        public static CancellationDecision Free() => new(true, 0m, null);

        public static CancellationDecision Chargeable(decimal fee) => new(true, fee, null);

        public static CancellationDecision Refuse(string reason) => new(false, 0m, reason);
    }

    public interface ICancellationPolicy
    {
        Task<CancellationDecision> Evaluate(Booking booking);
    }
}
