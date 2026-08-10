using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure.Entities;

namespace VetLims.Infrastructure.Services;

public class BarcodeService
{
    private readonly VetLimsDbContext _db;
    public BarcodeService(VetLimsDbContext db) { _db = db; }

    // Generates a unique code like "VL-00000123"
    public async Task<string> NextAsync(string prefix = "VL", CancellationToken ct = default)
    {
        // Single row table; create if missing
        var seq = await _db.BarcodeSequences.FirstOrDefaultAsync(ct);
        if (seq == null)
        {
            seq = new BarcodeSequence { Prefix = prefix, CurrentValue = 0 };
            _db.BarcodeSequences.Add(seq);
            await _db.SaveChangesAsync(ct);
        }

        seq.CurrentValue += 1;
        await _db.SaveChangesAsync(ct);

        return $"{seq.Prefix}-{seq.CurrentValue:D8}";
    }
}
