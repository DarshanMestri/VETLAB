using VetLims.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace VetLims.Infrastructure.Services;

// Resolves price by priority: Client-specific rate -> default (null client) rate -> 0
using VetLims.Infrastructure.Entities; // <-- for RegItemType

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

        if (price > 0) return price;

        var fallback = await _db.ClientRates.AsNoTracking()
            .Where(r => r.ItemType == type && r.ItemId == itemId && r.ClientId == null)
            .Select(r => r.Rate)
            .FirstOrDefaultAsync(ct);

        return fallback;
    }
}
