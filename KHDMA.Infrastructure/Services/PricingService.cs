using KHDMA.Application.Common;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KHDMA.Infrastructure.Services
{
    public class PricingService : IPricingService
    {
        private readonly AppDbContext _db;
        private readonly ICommissionService _commission;
        private readonly VatSettings _vat;

        public PricingService(AppDbContext db, ICommissionService commission, IOptions<VatSettings> vat)
        {
            _db = db;
            _commission = commission;
            _vat = vat.Value;
        }

        public async Task<PriceBreakdown?> ForServiceAsync(Guid serviceId)
        {
            var price = await _db.Services
                .AsNoTracking()
                .Where(s => s.id == serviceId && s.IsActive)
                .Select(s => s.FixedPrice)
                .FirstOrDefaultAsync();

            if (price is null) return null;
            return await ForServiceFeeAsync(price.Value);
        }

        public async Task<PriceBreakdown> ForServiceFeeAsync(decimal serviceFee)
        {
            var commissionRate = await GetCommissionRateAsync();
            var vatRate = _vat.Rate;

            // Round each component to 2dp, then derive the total from the rounded
            // parts. Rounding the total independently would let the displayed lines
            // fail to add up to the charged amount by a piastre.
            var vatAmount = Round(serviceFee * vatRate);
            var total = Round(serviceFee) + vatAmount;
            var commissionAmount = Round(serviceFee * commissionRate);
            var providerEarning = Round(serviceFee) - commissionAmount;

            return new PriceBreakdown(
                Round(serviceFee), vatRate, vatAmount, total,
                commissionRate, commissionAmount, providerEarning, _vat.Currency);
        }

        private async Task<decimal> GetCommissionRateAsync()
        {
            var response = await _commission.GetCurrentRateAsync();
            // 15% is the seeded CommissionSettings default (AppDbContext.OnModelCreating).
            return response.Success && response.Data is not null ? response.Data.Rate : 0.15m;
        }

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
