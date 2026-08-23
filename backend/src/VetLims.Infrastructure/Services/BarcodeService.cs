using System.Data;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure.Entities;

namespace VetLims.Infrastructure.Services;

public class BarcodeService
{
    private readonly VetLimsDbContext _db;
    public BarcodeService(VetLimsDbContext db) { _db = db; }

    /// <summary>
    /// Generates a unique sequential code like "VL-00000123".
    /// Each prefix maintains its own independent sequence.
    /// Uses row-level locking to prevent duplicates under concurrency.
    /// </summary>
    public async Task<string> NextAsync(string prefix = "VL", CancellationToken ct = default)
    {
        // Ensure a sequence row exists for this prefix
        var exists = await _db.BarcodeSequences.AnyAsync(x => x.Prefix == prefix, ct);
        if (!exists)
        {
            try
            {
                _db.BarcodeSequences.Add(new BarcodeSequence { Prefix = prefix, CurrentValue = 0 });
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Another thread inserted the same prefix — safe to continue
            }
        }

        // Atomic increment using raw ADO (PostgreSQL: UPDATE ... RETURNING).
        // SqlQueryRaw is not usable here: an UPDATE statement is non-composable,
        // so any LINQ operator over it throws, and scalar SqlQuery maps by column
        // name "Value" which RETURNING does not emit. ExecuteScalar avoids both.
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        using var cmd = conn.CreateCommand();
        // Table is snake_case but columns keep EF's PascalCase (quoted) names.
        cmd.CommandText =
            "UPDATE barcode_sequences SET \"CurrentValue\" = \"CurrentValue\" + 1 WHERE \"Prefix\" = @prefix RETURNING \"CurrentValue\"";
        var param = cmd.CreateParameter();
        param.ParameterName = "@prefix";
        param.Value = prefix;
        cmd.Parameters.Add(param);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var result = scalar == null || scalar == DBNull.Value ? 0L : Convert.ToInt64(scalar);

        if (result == 0)
        {
            // Sequence row was deleted — recreate and retry
            _db.BarcodeSequences.Add(new BarcodeSequence { Prefix = prefix, CurrentValue = 1 });
            await _db.SaveChangesAsync(ct);
            result = 1;
        }

        return $"{prefix}-{result:D8}";
    }
}
