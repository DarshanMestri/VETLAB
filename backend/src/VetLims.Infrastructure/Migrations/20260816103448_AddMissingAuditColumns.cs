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
            migrationBuilder.Sql(@"ALTER TABLE ""laboratory_groups"" ADD COLUMN ""CreatedAt"" timestamp with time zone NULL;");
            migrationBuilder.Sql(@"UPDATE ""laboratory_groups"" SET ""CreatedAt"" = ""CreatedDate"";");
            migrationBuilder.Sql(@"ALTER TABLE ""laboratory_groups"" ALTER COLUMN ""CreatedAt"" SET NOT NULL;");

            migrationBuilder.Sql(@"ALTER TABLE ""laboratory_groups"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NULL;");
            migrationBuilder.Sql(@"UPDATE ""laboratory_groups"" SET ""UpdatedAt"" = ""ModifiedDate"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""laboratory_groups"" DROP COLUMN ""CreatedAt"";");
            migrationBuilder.Sql(@"ALTER TABLE ""laboratory_groups"" DROP COLUMN ""UpdatedAt"";");
        }
    }
}
