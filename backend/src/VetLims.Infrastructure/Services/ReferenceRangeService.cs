using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure.Entities;

namespace VetLims.Infrastructure.Services;

public class ReferenceRangeService
{
    private readonly VetLimsDbContext _db;
    public ReferenceRangeService(VetLimsDbContext db) { _db = db; }

    // Picks the most specific range by parameter + species + ageDays
    public async Task<(int? minDays, int? maxDays, decimal? low, decimal? high)> 
        PickRangeAsync(int testParameterId, int? speciesId, int? ageDays, CancellationToken ct = default)
    {
        if (speciesId is null) return (null, null, null, null);

        // Find ranges for given parameter & species where ageDays falls inside
        var q = _db.TestReferenceRanges.AsNoTracking()
            .Where(r => r.TestParameterId == testParameterId && r.SpeciesId == speciesId);

        if (ageDays.HasValue)
        {
            q = q.Where(r =>
                (!r.AgeFromDays.HasValue || r.AgeFromDays.Value <= ageDays.Value) &&
                (!r.AgeToDays.HasValue   || r.AgeToDays.Value   >= ageDays.Value));
        }

        var best = await q
            .OrderByDescending(r => r.AgeFromDays ?? int.MinValue)
            .ThenBy(r => r.AgeToDays ?? int.MaxValue)
            .FirstOrDefaultAsync(ct);

        if (best == null) return (null, null, null, null);

        return (best.AgeFromDays, best.AgeToDays, best.Low, best.High);
    }

    public (bool? isLow, bool? isHigh, bool? outOf)
        Evaluate(decimal? value, decimal? low, decimal? high)
    {
        if (value is null || (low is null && high is null)) return (null, null, null);
        bool isLow = low.HasValue && value.Value < low.Value;
        bool isHigh = high.HasValue && value.Value > high.Value;
        bool outOf = isLow || isHigh;
        return (isLow ? true : (bool?)null, isHigh ? true : (bool?)null, outOf ? true : (bool?)null);
    }
}
