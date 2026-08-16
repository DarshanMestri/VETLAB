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
        var price = await _db.ClientRates.AsNoTracking()
            .Where(r => r.ItemType == type && r.ItemId == itemId && r.ClientId == clientId)
            .Select(r => r.Rate)
            .FirstOrDefaultAsync(ct);

        return price > 0 ? price : 0;
    }
}
