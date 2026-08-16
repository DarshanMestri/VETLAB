using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Step4B_FixesAndBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    active_org_count integer;
                    legacy_org_id integer;
                BEGIN
                    SELECT COUNT(*) INTO active_org_count
                    FROM organizations
                    WHERE "IsDeleted" = false AND "IsActive" = true;

                    IF active_org_count = 0 THEN
                        RAISE EXCEPTION 'No active organization exists to receive legacy tests';
                    END IF;

                    IF active_org_count > 1 THEN
                        SELECT "Id" INTO legacy_org_id
                        FROM organizations
                        WHERE "IsDeleted" = false AND "IsActive" = true
                        ORDER BY "Id"
                        LIMIT 1;

                        IF EXISTS (SELECT 1 FROM tests WHERE "OrganizationId" IS NULL OR "OrganizationId" = 0) THEN
                            RAISE EXCEPTION 'Multiple organizations exist; existing tests cannot be assigned safely without a business mapping';
                        END IF;
                    ELSE
                        SELECT "Id" INTO legacy_org_id
                        FROM organizations
                        WHERE "IsDeleted" = false AND "IsActive" = true
                        ORDER BY "Id"
                        LIMIT 1;
                    END IF;

                    UPDATE tests
                    SET "OrganizationId" = legacy_org_id
                    WHERE "OrganizationId" IS NULL OR "OrganizationId" = 0;
                END $$;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_tests_organizations_OrganizationId",
                table: "tests",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tests_organizations_OrganizationId",
                table: "tests");
        }
    }
}
