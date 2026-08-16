using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill compatibility audit columns (CreatedAt/UpdatedAt) that the
            // model maps via BaseEntity but were missing from these two tables.
            foreach (var table in new[] { "laboratory_groups", "user_locations" })
            {
                migrationBuilder.Sql($@"ALTER TABLE ""{table}"" ADD COLUMN ""CreatedAt"" timestamp with time zone NULL;");
                migrationBuilder.Sql($@"UPDATE ""{table}"" SET ""CreatedAt"" = ""CreatedDate"";");
                migrationBuilder.Sql($@"ALTER TABLE ""{table}"" ALTER COLUMN ""CreatedAt"" SET NOT NULL;");

                migrationBuilder.Sql($@"ALTER TABLE ""{table}"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NULL;");
                migrationBuilder.Sql($@"UPDATE ""{table}"" SET ""UpdatedAt"" = ""ModifiedDate"";");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "laboratory_groups", "user_locations" })
            {
                migrationBuilder.Sql($@"ALTER TABLE ""{table}"" DROP COLUMN ""CreatedAt"";");
                migrationBuilder.Sql($@"ALTER TABLE ""{table}"" DROP COLUMN ""UpdatedAt"";");
            }
        }
    }
}
