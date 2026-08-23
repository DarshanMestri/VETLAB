using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillWalkinOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Create an owner master for each distinct walk-in identity
            //    (trimmed name + coalesced mobile) that has no active owner yet.
            //    Codes continue after the highest existing OWN-xxxx number.
            migrationBuilder.Sql(@"
INSERT INTO owners (""Code"", ""Name"", ""Mobile"", ""NameAr"", ""IsActive"", ""IsDeleted"", ""CreatedDate"", ""CreatedAt"")
SELECT 'OWN-' || LPAD(((SELECT COALESCE(MAX(SUBSTRING(x.""Code"" FROM '[0-9]+')::bigint), 0) FROM owners x)
        + ROW_NUMBER() OVER (ORDER BY o.""Name"", o.""Mobile""))::text, 8, '0'),
       o.""Name"",
       NULLIF(o.""Mobile"", ''),
       (SELECT r.""OwnerNameAr"" FROM registrations r
          WHERE NOT r.""IsDeleted"" AND r.""OwnerId"" IS NULL
            AND TRIM(r.""OwnerName"") = o.""Name""
            AND COALESCE(TRIM(r.""OwnerMobile""), '') = o.""Mobile""
            AND r.""OwnerNameAr"" IS NOT NULL
          ORDER BY r.""CreatedAt"" LIMIT 1),
       TRUE, FALSE, NOW(), NOW()
FROM (
    SELECT DISTINCT TRIM(r.""OwnerName"") AS ""Name"",
           COALESCE(TRIM(r.""OwnerMobile""), '') AS ""Mobile""
    FROM registrations r
    WHERE NOT r.""IsDeleted"" AND r.""OwnerId"" IS NULL AND TRIM(COALESCE(r.""OwnerName"", '')) <> ''
) o
WHERE NOT EXISTS (
    SELECT 1 FROM owners x2
    WHERE x2.""Name"" = o.""Name""
      AND COALESCE(x2.""Mobile"", '') = o.""Mobile""
      AND NOT x2.""IsDeleted""
);");

            // 2) Link registrations to their owner master via exact trimmed name +
            //    null-safe mobile match. Only rows still unlinked are touched.
            migrationBuilder.Sql(@"
UPDATE registrations r
SET ""OwnerId"" = x.""Id""
FROM owners x
WHERE NOT r.""IsDeleted""
  AND r.""OwnerId"" IS NULL
  AND x.""Name"" = TRIM(r.""OwnerName"")
  AND COALESCE(x.""Mobile"", '') = COALESCE(TRIM(r.""OwnerMobile""), '')
  AND NOT x.""IsDeleted"";");

            // 3) Keep the OWN barcode sequence ahead of every existing code so the
            //    next generated code never collides with a soft-deleted row.
            migrationBuilder.Sql(@"
UPDATE barcode_sequences
SET ""CurrentValue"" = GREATEST(""CurrentValue"",
        (SELECT COALESCE(MAX(SUBSTRING(""Code"" FROM '[0-9]+')::bigint), 0) FROM owners))
WHERE ""Prefix"" = 'OWN';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill — no structural change to undo.
        }
    }
}
