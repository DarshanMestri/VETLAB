using VetLims.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace VetLims.Infrastructure.Services;

// Resolves a client-specific price, or zero when no rate is configured.
public class PricingService
{
    private readonly VetLimsDbContext _db;
    public PricingService(VetLimsDbContext db) { _db = db; }

    public async Task<decimal> ResolvePriceAsync(int? clientId, RegItemType type, int itemId, CancellationToken ct = default)
    {
        var (price, _) = await ResolvePricingAsync(clientId, type, itemId, ct);
        return price;
    }

    public async Task<(decimal Price, decimal VatRate)> ResolvePricingAsync(int? clientId, RegItemType type, int itemId, CancellationToken ct = default)
    {
        var rate = await _db.ClientRates.AsNoTracking()
            .Where(r => r.ItemType == type && r.ItemId == itemId && r.ClientId == clientId)
            .Select(r => new { r.Rate, r.VatRate })
            .FirstOrDefaultAsync(ct);

        var price = rate?.Rate > 0 ? rate.Rate : 0;
        return (price, rate?.VatRate ?? 0m);
    }
}
