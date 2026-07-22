namespace KHDMA.Application.Interfaces.Services
{
    /// <summary>
    /// One authoritative money calculation, reused by booking creation, the
    /// dispatch job card, payment capture and the earnings report.
    /// </summary>
    /// <param name="ServiceFee">The admin-set service price, before VAT.</param>
    /// <param name="VatAmount">VAT on the service fee. Collected for the tax authority - not platform revenue.</param>
    /// <param name="Total">What the customer pays: ServiceFee + VatAmount.</param>
    /// <param name="CommissionAmount">The platform's cut, taken off the service fee only.</param>
    /// <param name="ProviderEarning">What the provider is paid: ServiceFee - CommissionAmount.</param>
    public record PriceBreakdown(
        decimal ServiceFee,
        decimal VatRate,
        decimal VatAmount,
        decimal Total,
        decimal CommissionRate,
        decimal CommissionAmount,
        decimal ProviderEarning,
        string Currency);

    /// <summary>
    /// Computes booking money server-side.
    /// </summary>
    /// <remarks>
    /// Commission is taken off the <em>service fee</em>, never off the VAT-inclusive
    /// total. Charging commission on VAT would mean the platform takes a cut of
    /// money that belongs to the tax authority.
    /// </remarks>
    public interface IPricingService
    {
        /// <summary>Snapshots the current price of a service. Never trusts a client-supplied amount.</summary>
        Task<PriceBreakdown?> ForServiceAsync(Guid serviceId);

        /// <summary>Recomputes a breakdown from an already-snapshotted service fee.</summary>
        Task<PriceBreakdown> ForServiceFeeAsync(decimal serviceFee);
    }
}
